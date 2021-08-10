﻿using System.Collections.Generic;
using CodeGenerationRoslynTest.Generators.Interfaces;
using CodeGenerationRoslynTest.Models.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using CodeGenerationRoslynTest.Models.VisualStudio;
using CodeGenerationRoslynTest.Models.Config;
using System.Linq;
using CodeGenerationRoslynTest.Exceptions;
using System;

namespace CodeGenerationRoslynTest.Generators
{
    public class EntityGenerator : IGenerator
    {
        public void Generate(CommandLineConfiguration cliConfig, VSProjectMetadata projectMetadata, Database database)
        {
            var entitiesDirectory = new DirectoryInfo($"{projectMetadata.BasePath}/{cliConfig.EntitiesFolder}");
            var shouldGenerate = false;

            if(entitiesDirectory.Exists)
            {
                //Entities have already been created or files exist in the target folder.
                if(DirectoryHasFiles(entitiesDirectory.FullName))
                {
                    if(!cliConfig.ForceRecreate)
                    {
                        throw new CodeGenerationException("To be able to run the generation routine when you have previously generated code, you must set ForceRecreate to true or your target directories must be empty.");
                    }
                    else
                    {
                        shouldGenerate = true;
                        //Delete existing files.
                        foreach (var file in entitiesDirectory.EnumerateFiles())
                        {
                            file.Delete();
                        }
                    }
                }
                else
                    shouldGenerate = true;
            }
            else
            {
                Directory.CreateDirectory($"{projectMetadata.BasePath}/{cliConfig.EntitiesFolder}");
                shouldGenerate = true;
            }

            if(shouldGenerate)
            {
                foreach (var tableMeta in database.Tables)
                {
                    var entityCode = GenerateEntity(cliConfig.EntitiesFolder, tableMeta, projectMetadata);
                    WriteEntityFileToDisk($"{projectMetadata.BasePath}/{cliConfig.EntitiesFolder}/{tableMeta.FormattedTableName}.cs", entityCode);
                }
            }
        }

        private string GenerateEntity(string entitiesFolder, Table tableMetadata, VSProjectMetadata projectMetadata)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();

            var usings = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System"));

            compilationUnit = compilationUnit.AddUsings(usings);

            var entityNamespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{projectMetadata.DefaultNamespace}.{entitiesFolder}")).NormalizeWhitespace();

            var entityClass = SyntaxFactory.ClassDeclaration(tableMetadata.FormattedTableName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            foreach (var columnMeta in tableMetadata.Columns)
            {
                var columnType = columnMeta.Type.CSharpTypeString;
                var columnName = columnMeta.FormattedColumnName;
                var databaseType = columnMeta.Type.DatabaseType;

                var property = SyntaxFactory.ParseMemberDeclaration($"public {columnType} {columnName} {{ get; set; }}");

                if(property != null && !string.IsNullOrWhiteSpace(columnType)) { 
                    entityClass = entityClass.AddMembers(property);
                }
                else
                {
                    Console.WriteLine("Skipping Column Generation");
                    Console.WriteLine($"Table Column belongs to: {tableMetadata.TableName}. Column: {columnName}. Database type {databaseType} not mapped.");
                }
            }

            entityNamespace = entityNamespace.AddMembers(entityClass);

            compilationUnit = compilationUnit.AddMembers(entityNamespace);

            var code = compilationUnit
                .NormalizeWhitespace()
                .ToFullString();

            return code;
        }

        private void WriteEntityFileToDisk(string file, string code)
        {
            using(var sourceWriter = new StreamWriter(file))
            {
                sourceWriter.WriteLine(code);
            }
        }

        private bool DirectoryHasFiles(string path)
        {
            return Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
