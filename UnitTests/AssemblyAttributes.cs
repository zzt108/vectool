// UnitTests/AssemblyAttributes.cs
using NUnit.Framework;

// Ensure every test runs under Single-Threaded Apartment (STA) for WinForms/OLE drag-drop
[assembly: Apartment(ApartmentState.STA)]

// Keep a single test worker to avoid multiple STA workers fighting over UI resources
[assembly: LevelOfParallelism(1)]
