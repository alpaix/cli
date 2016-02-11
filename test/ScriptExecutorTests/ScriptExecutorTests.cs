﻿using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli.Utils.ScriptExecutorTests
{
    public class ScriptExecutorTests : TestBase
    {
        private static readonly string s_testProjectRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets/TestProjects");

        private TempDirectory _root;

        public ScriptExecutorTests()
        {
            _root = Temp.CreateDirectory();
        }

        [Fact]
        public void Test_Project_Local_Script_is_Resolved()
        {
            var sourceTestProjectPath = Path.Combine(s_testProjectRoot, "TestApp");
            var binTestProjectPath = _root.CopyDirectory(sourceTestProjectPath).Path;

            var project = ProjectContext.Create(binTestProjectPath, NuGetFramework.Parse("dnxcore50")).ProjectFile;

            CreateTestFile("some.script", binTestProjectPath);
            var scriptCommandLine = "some.script";

            var command = ScriptExecutor.CreateCommandForScript(project, scriptCommandLine, new Dictionary<string, string>());

            command.Should().NotBeNull();
            command.ResolutionStrategy.Should().Be(CommandResolutionStrategy.ProjectLocal);
        }
        
        [Fact]
        public void Test_Nonexistent_Project_Local_Script_is_not_Resolved()
        {
            var sourceTestProjectPath = Path.Combine(s_testProjectRoot, "TestApp");
            var binTestProjectPath = _root.CopyDirectory(sourceTestProjectPath).Path;

            var project = ProjectContext.Create(binTestProjectPath, NuGetFramework.Parse("dnxcore50")).ProjectFile;

            var scriptCommandLine = "nonexistent.script";

            Action action = () => ScriptExecutor.CreateCommandForScript(project, scriptCommandLine, new Dictionary<string, string>());
            action.ShouldThrow<CommandUnknownException>();
        }
        
        [Theory]
        [InlineData(".sh")]
        [InlineData(".cmd")]
        public void Test_Extension_Inference_in_Resolution_for_Project_Local_Scripts(string extension)
        {
            var sourceTestProjectPath = Path.Combine(s_testProjectRoot, "TestApp");
            var binTestProjectPath = _root.CopyDirectory(sourceTestProjectPath).Path;

            var project = ProjectContext.Create(binTestProjectPath, NuGetFramework.Parse("dnxcore50")).ProjectFile;

            CreateTestFile("script" + extension, binTestProjectPath);

            // Don't include extension
            var scriptCommandLine = "script";

            var command = ScriptExecutor.CreateCommandForScript(project, scriptCommandLine, new Dictionary<string, string>());

            command.Should().NotBeNull();
            command.ResolutionStrategy.Should().Be(CommandResolutionStrategy.ProjectLocal);
        }

        [Fact]
        public void Test_Script_Exe_Files_Dont_Use_Cmd_or_Sh()
        {
            var sourceTestProjectPath = Path.Combine(s_testProjectRoot, "TestApp");
            var binTestProjectPath = _root.CopyDirectory(sourceTestProjectPath).Path;

            var project = ProjectContext.Create(binTestProjectPath, NuGetFramework.Parse("dnxcore50")).ProjectFile;

            CreateTestFile("some.exe", binTestProjectPath);
            var scriptCommandLine = "some.exe";

            var command = ScriptExecutor.CreateCommandForScript(project, scriptCommandLine, new Dictionary<string, string>());

            command.Should().NotBeNull();
            command.ResolutionStrategy.Should().Be(CommandResolutionStrategy.ProjectLocal);

            Path.GetFileName(command.CommandName).Should().NotBe("cmd.exe");
            Path.GetFileName(command.CommandName).Should().NotBe("sh");
        }
        
        [Fact]
        public void Test_Script_Builtins_Fail()
        {
            var sourceTestProjectPath = Path.Combine(s_testProjectRoot, "TestApp");
            var binTestProjectPath = _root.CopyDirectory(sourceTestProjectPath).Path;

            var project = ProjectContext.Create(binTestProjectPath, NuGetFramework.Parse("dnxcore50")).ProjectFile;

            var scriptCommandLine = "echo";

            Action action = () => ScriptExecutor.CreateCommandForScript(project, scriptCommandLine, new Dictionary<string, string>());
            action.ShouldThrow<CommandUnknownException>();
        }

        private void CreateTestFile(string filename, string directory)
        {
            string path = Path.Combine(directory, filename);
            File.WriteAllText(path, "echo hello");
        }
    }
}
