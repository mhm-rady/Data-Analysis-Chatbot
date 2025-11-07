using System.Text;
using Microsoft.Extensions.Logging;
using SsasMcpServer.Models.Query;
using SsasMcpServer.Services.Interfaces;

namespace SsasMcpServer.Services.Implementations;

public class DaxQueryBuilder : IDaxQueryBuilder
{
    private readonly ILogger<DaxQueryBuilder> _logger;

    public DaxQueryBuilder(ILogger<DaxQueryBuilder> logger)
    {
        _logger = logger;
    }

    public string BuildQuery(QueryRequest request)
    {
        _logger.LogDebug("Building DAX query from request");

        var sb = new StringBuilder();
        
        // Build EVALUATE clause
        sb.AppendLine("EVALUATE");
        
        // Determine if we need to use SUMMARIZECOLUMNS or simple table reference
        if (request.Measures.Count > 0 || request.Filters.Count > 0)
        {
            sb.AppendLine(BuildSummarizeColumnsQuery(request));
        }
        else
        {
            sb.AppendLine(BuildSimpleTableQuery(request));
        }

        // Build ORDER BY clause if needed
        if (request.Sorts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(BuildOrderByClause(request.Sorts));
        }

        var query = sb.ToString();
        _logger.LogDebug("Generated DAX query: {Query}", query);
        
        return query;
    }

    private string BuildSummarizeColumnsQuery(QueryRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("SUMMARIZECOLUMNS(");

        // Add dimension columns
        if (request.Columns.Count > 0)
        {
            sb.AppendLine();
            for (int i = 0; i < request.Columns.Count; i++)
            {
                sb.Append("    ");
                sb.Append(EscapeDaxIdentifier(request.Columns[i]));
                if (i < request.Columns.Count - 1 || request.Filters.Count > 0 || request.Measures.Count > 0)
                    sb.Append(",");
                sb.AppendLine();
            }
        }

        // Add filters
        if (request.Filters.Count > 0)
        {
            foreach (var filter in request.Filters)
            {
                sb.Append("    ");
                sb.Append(BuildFilterTable(filter));
                if (filter != request.Filters.Last() || request.Measures.Count > 0)
                    sb.Append(",");
                sb.AppendLine();
            }
        }

        // Add measures
        if (request.Measures.Count > 0)
        {
            for (int i = 0; i < request.Measures.Count; i++)
            {
                sb.Append("    ");
                sb.Append($"\"{GetMeasureDisplayName(request.Measures[i])}\", {EscapeDaxIdentifier(request.Measures[i])}");
                if (i < request.Measures.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
        }

        sb.Append(")");

        // Apply TopN if specified
        if (request.TopN.HasValue && request.TopN.Value > 0)
        {
            sb.Insert(0, $"TOPN({request.TopN.Value}, ");
            
            // Add default sort if no sorts specified
            if (request.Sorts.Count == 0 && request.Measures.Count > 0)
            {
                sb.Append($", {EscapeDaxIdentifier(request.Measures[0])}, DESC");
            }
            else if (request.Sorts.Count > 0)
            {
                var firstSort = request.Sorts[0];
                sb.Append($", {EscapeDaxIdentifier(firstSort.Column)}, {(firstSort.Direction == SortDirection.Descending ? "DESC" : "ASC")}");
            }
            
            sb.Append(")");
        }

        return sb.ToString();
    }

    private string BuildSimpleTableQuery(QueryRequest request)
    {
        if (request.Columns.Count == 0)
            throw new ArgumentException("At least one column must be specified");

        // Extract table name from first column
        var tableName = ExtractTableName(request.Columns[0]);
        var sb = new StringBuilder();

        if (request.TopN.HasValue && request.TopN.Value > 0)
        {
            sb.Append($"TOPN({request.TopN.Value}, ");
        }

        if (request.Columns.Count == 1)
        {
            // Single column - use VALUES
            sb.Append($"VALUES({EscapeDaxIdentifier(request.Columns[0])})");
        }
        else
        {
            // Multiple columns - use SELECTCOLUMNS
            sb.Append($"SELECTCOLUMNS({EscapeDaxIdentifier(tableName)}");
            foreach (var column in request.Columns)
            {
                var columnName = ExtractColumnName(column);
                sb.Append($", \"{columnName}\", {EscapeDaxIdentifier(column)}");
            }
            sb.Append(")");
        }

        if (request.TopN.HasValue && request.TopN.Value > 0)
        {
            sb.Append(")");
        }

        return sb.ToString();
    }

    private string BuildFilterTable(QueryFilter filter)
    {
        var filterExpression = BuildFilterExpression(filter);
        return $"FILTER(ALL({EscapeDaxIdentifier(filter.Column)}), {filterExpression})";
    }

    public string BuildFilterExpression(QueryFilter filter)
    {
        var column = EscapeDaxIdentifier(filter.Column);
        var value = FormatDaxValue(filter.Value, filter.Operator);

        return filter.Operator switch
        {
            FilterOperator.Equals => $"{column} = {value}",
            FilterOperator.NotEquals => $"{column} <> {value}",
            FilterOperator.GreaterThan => $"{column} > {value}",
            FilterOperator.GreaterThanOrEquals => $"{column} >= {value}",
            FilterOperator.LessThan => $"{column} < {value}",
            FilterOperator.LessThanOrEquals => $"{column} <= {value}",
            FilterOperator.Contains => $"SEARCH({value}, {column}, 1, 0) > 0",
            FilterOperator.StartsWith => $"LEFT({column}, LEN({value})) = {value}",
            FilterOperator.EndsWith => $"RIGHT({column}, LEN({value})) = {value}",
            FilterOperator.In => BuildInExpression(column, filter.Value),
            FilterOperator.NotIn => $"NOT({BuildInExpression(column, filter.Value)})",
            _ => throw new ArgumentException($"Unsupported filter operator: {filter.Operator}")
        };
    }

    private string BuildInExpression(string column, object value)
    {
        if (value is not IEnumerable<object> values)
            throw new ArgumentException("IN operator requires a collection of values");

        var valueList = string.Join(", ", values.Select(v => FormatDaxValue(v, FilterOperator.In)));
        return $"{column} IN {{{valueList}}}";
    }

    public string BuildOrderByClause(List<QuerySort> sorts)
    {
        if (sorts.Count == 0)
            return string.Empty;

        var sb = new StringBuilder("ORDER BY");

        for (int i = 0; i < sorts.Count; i++)
        {
            sb.AppendLine();
            sb.Append("    ");
            sb.Append(EscapeDaxIdentifier(sorts[i].Column));
            sb.Append(sorts[i].Direction == SortDirection.Descending ? " DESC" : " ASC");
            
            if (i < sorts.Count - 1)
                sb.Append(",");
        }

        return sb.ToString();
    }

    public string EscapeDaxIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty");

        // If already properly formatted with table[column] or [measure], return as is
        if (identifier.Contains('[') && identifier.Contains(']'))
        {
            return identifier;
        }

        // If it contains a dot, split into table and column
        if (identifier.Contains('.'))
        {
            var parts = identifier.Split('.');
            if (parts.Length == 2)
            {
                return $"'{parts[0].Trim()}'[{parts[1].Trim()}]";
            }
        }

        // Check if it needs table prefix (contains special characters or spaces)
        if (NeedsQuoting(identifier))
        {
            return $"[{identifier}]";
        }

        return identifier;
    }

    public string FormatDaxValue(object value, FilterOperator op)
    {
        if (value == null)
            return "BLANK()";

        return value switch
        {
            string str => $"\"{EscapeString(str)}\"",
            DateTime dt => $"DATE({dt.Year}, {dt.Month}, {dt.Day})",
            DateTimeOffset dto => $"DATE({dto.Year}, {dto.Month}, {dto.Day})",
            bool b => b.ToString().ToUpper(),
            decimal or double or float => Convert.ToDouble(value).ToString(System.Globalization.CultureInfo.InvariantCulture),
            int or long or short => value.ToString()!,
            _ => $"\"{value}\""
        };
    }

    private string EscapeString(string str)
    {
        return str.Replace("\"", "\"\"");
    }

    private bool NeedsQuoting(string identifier)
    {
        // Check if identifier contains special characters or spaces
        return identifier.Any(c => !char.IsLetterOrDigit(c) && c != '_');
    }

    private string ExtractTableName(string column)
    {
        // Handle Table[Column] format
        if (column.Contains('['))
        {
            var idx = column.IndexOf('[');
            return column.Substring(0, idx).Trim().Trim('\'');
        }

        // Handle Table.Column format
        if (column.Contains('.'))
        {
            return column.Split('.')[0].Trim();
        }

        return column;
    }

    private string ExtractColumnName(string column)
    {
        // Handle Table[Column] format
        if (column.Contains('[') && column.Contains(']'))
        {
            var start = column.IndexOf('[') + 1;
            var end = column.IndexOf(']');
            return column.Substring(start, end - start);
        }

        // Handle Table.Column format
        if (column.Contains('.'))
        {
            return column.Split('.')[1].Trim();
        }

        return column;
    }

    private string GetMeasureDisplayName(string measure)
    {
        // Extract display name from [MeasureName] format
        if (measure.Contains('[') && measure.Contains(']'))
        {
            return ExtractColumnName(measure);
        }

        // If already a simple name, return it
        return measure;
    }
}