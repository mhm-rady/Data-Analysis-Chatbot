## Introduction
This project with business users to ask analytics question by chatting with AI agent that will handle querying the analysis services and format the response that match user expectations.

## Project Main Components
1. Front-end: web-based GUI for user query input and streaming response.
2. Back-end: multi Agent framework for receiving user inquiries and start a workflow of analyzing and tool calling for analysis services then formatting the output.
3. LLMs: use small local models for local deployments.
4. MCP Server: support tools that will handle LLMs queries for the analysis service.

## Business workflow
1. User asks a business question for a specific case.
2. Analyzer Agent process user input as following
	1. Understand business requirements.
	2. Insure that analysis service model can cover the requirements.
	3. Decide whether to build analytical query items (dimension columns, measures, filters ...) or give feedback to the business user.
3. Executer agent use query items to execute analysis services query and get results using right MCP tools.
4. The evaluator agent check that result is matching user business requirements or need to execute more queries.
5. The formatter agent understand business requirement and design best output form that would meet the user expectations.

## Technology Stack
1. Analysis Service: SQL Server Analysis Services (SSAS)
2. Agentic Framework: LangGraph with python, for building multi agent workflows.
3. MCP Server: .NET Framwork using C#, offering tool for quering SSAS tabular models.
4. Front-End: Gradio, web-base GUI for chatpot and analytical result vizulizers.
5. Deployment: Docker container

## Project Plan
1. Create project services architecture.
2. Design SSAS tabular model metadata structure.
3. Design MCP server tools structure.
4. Design LangGraph node agentic workflow.
5. Design output result templates and formats.
6. Implement MCP server tools.
7. Implement agentic workflow with LLM context.
8. Implement frontend GUI.
9. Prepare deployment packaging.

## General Roles
1. Chatbot should support multi-languages and respond with same user input language.
## SSAS Metadata
The MCP server should provide a tools for LLM to retrieve tabular model metadata so the LLM model can use it for assessment user business requirements for building query items or give feedback to the user.
1. SSAS Metadata should include: dimensions (tables),  attributes (columns), measures, hierarchies and KPIs.
2. metadata should have object technical name and description if exist.
3. metadata should include different cultures if exists in SSAS model for name and description to help butter understanding business requirements in native languages. 

## MCP Server
Acting as an mediator between agent LLM and SSAS database by providing tools for retrieving SSAS model metadata and execute analytical queries.
1. Use .NET framework with C# for native support with SSAS server.
2. Write full .NET cs project with best practices as MCP server.
3. Support VSCode development IDE.
4. Follow design patterns in project implementation.
5. Support container deployment such as Docker.
6. Include SSAS Metadata tools.
7. Include executing query wrapper tools by accepting required columns, measures, filters, sort fields and result row limit.
8. Writing DAX queries should be inside MCP tools to avoid LLM generate AD hock DAX queries and depends on existing model items to avoid misleading information.
