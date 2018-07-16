﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure
{
    public static class MvcTestSource
    {
        private static readonly string ProjectDirectory = GetProjectDirectory();

        public static TestSource Read(string testClassName, string testMethod)
        {
            var filePath = Path.Combine(ProjectDirectory, "TestFiles", testClassName, testMethod + ".cs");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"TestFile {testMethod} could not be found at {filePath}.", filePath);
            }

            var fileContent = File.ReadAllText(filePath)
                .Replace("_INPUT_", "_TEST_", StringComparison.Ordinal)
                .Replace("_OUTPUT_", "_TEST_", StringComparison.Ordinal);

            return TestSource.Read(fileContent);
        }

        private static string GetProjectDirectory()
        {
            var solutionDirectory = TestPathUtilities.GetSolutionRootDirectory("Mvc");
            var assemblyName = typeof(MvcTestSource).Assembly.GetName().Name;
            var projectDirectory = Path.Combine(solutionDirectory, "test", assemblyName);
            return projectDirectory;
        }
    }
}
