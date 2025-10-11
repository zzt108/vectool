
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Allow the UnitTests project to access internal members of the UI assembly.
[assembly: InternalsVisibleTo("VecTool.WinUI.Tests")] //VecTool.WinUI.TestMS
[assembly: InternalsVisibleTo("VecTool.WinUI.TestMS")] //VecTool.WinUI.TestMS

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]