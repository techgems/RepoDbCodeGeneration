﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbReflector.Common.CommandModels;
using DbReflector.Common;
using Microsoft.Extensions.Hosting;
using DbReflector.Core.DI;
using DbReflector.Core;

namespace DbReflector.CLI
{
    //TO DO: Add DI.
    public class Program
    {
        public Program(IOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).ConfigureServices(services =>
            {
                services.AddDbReflector();
            });
        }

        static SupportedDatabases MatchDbEngineString(string dbEngine)
        {
            SupportedDatabases databaseEngine = SupportedDatabases.SqlServer;

            switch (dbEngine)
            {
                case "SQL Server":
                case "mssql":
                    databaseEngine = SupportedDatabases.SqlServer;
                    break;
                case "pgsql":
                case "Postgres":
                    databaseEngine = SupportedDatabases.Postgres;
                    break;
            }

            return databaseEngine;
        }

        static void Reflect(string dbName, string projectPath, string connectionString, string dbEngine, bool force, List<string> tablesToIgnore, IConsole console)
        {
            SupportedDatabases databaseEngine = MatchDbEngineString(dbEngine);

            var commandModel = new ReflectCommandModel()
            {
                DatabaseName = dbName,
                CSharpProjectFilePath = projectPath,
                EntitiesFolder = "Entities",
                TablesToIgnore = tablesToIgnore,
                ConnectionString = connectionString,
                CaseToOutput =  EntityOutputCasing.PascalCase,
                ForceRecreate = force,
                DatabaseEngine = databaseEngine
            };
        }

        static void Scan(string dbName, string connectionString, string outputFolder, string dbEngine, bool forceOverride, IConsole console)
        {
            SupportedDatabases databaseEngine = MatchDbEngineString(dbEngine);

            var commandModel = new ScanCommandModel()
            {
                DatabaseName = dbName,
                ConnectionString = connectionString,
                OutputFolder = outputFolder,
                ForceOverride = forceOverride,
                DbEngine = databaseEngine
            };

            //Execute command.
        }

        public static async Task<int> Main(string[] args)
        {
            //Setup DI.
            var host = CreateHostBuilder(args).Build();

            var rootCommand = new RootCommand();
            rootCommand.Name = "db-reflector";
            rootCommand.Description = "A productivity tool for automating the writing of boring Data Access code.";

            var scanCommand = new Command("scan") {
                new Option<string>("-db")
                {
                    Description = "The name of the database to scan.",
                    Required = true
                },
                new Option<string>("-c")
                {
                    Description = "The connection string to utilize.",
                    Required = true
                },
                new Option<string>("-o")
                {
                    Description = "The directory to store the database JSON model.",
                    Required = true
                },
                new Option<string>("-e", getDefaultValue: () => "SQL Server")
                {
                    Description = "The database engine that you want to scan. SQL Server is assumed when this option is omitted.",
                    Required = false
                },

                new Option<bool>("-f", getDefaultValue: () => false)
                {
                    Description = "If a model is already stored in disk, this flag must be set to true to override the previous model.",
                    Required = false
                }
            };

            scanCommand.Handler = CommandHandler.Create<string, string, string, string, bool, IConsole>(Scan);
            rootCommand.Add(scanCommand);

            var reflectCommand = new Command("reflect")
            {
                new Option<string>("-db")
                {
                    Description = "Database name.",
                    Required = true
                },
                new Option<string>("-c")
                {
                    Description = "Connection string.",
                    Required = true
                },
                new Option<string>("-csproj")
                {
                    Description = "C# Project path.",
                    Required = true
                },
                new Option<string>("-e", getDefaultValue: () => "SQL Server")
                {
                    Description = "The database engine that you want to scan. SQL Server is assumed when this option is omitted.",
                    Required = false
                },
                new Option<bool>("-f", getDefaultValue: () => false)
                {
                    Description = "If a model is already stored in disk, this flag must be set to true to override the previous model.",
                    Required = false
                },
                new Option<List<string>>("-i", getDefaultValue: () => new List<string>())
                {
                    Description = "List of tables to ignore",
                    Required = false
                }
            };

            reflectCommand.Handler = CommandHandler.Create<string, string, string, string, bool, List<string>, IConsole>(Reflect);
            rootCommand.Add(reflectCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private readonly IOrchestrator _orchestrator;

        
    }
}
