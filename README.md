# VecTool: AI-Ready Code Export & Development Workflow Manager

**Version**: 4.5.25.1101  
**Framework**: .NET 8.0 Windows (WinForms)  
**License**: Apache 2.0  
**Status**: Production-Ready with Active Development

---

## 🎯 Overview

VecTool is a modular C# WinForms desktop application designed to export code projects in AI-digestible formats and manage Git-based development workflows. It bridges the gap between local development environments and large language models (LLMs).

### 📚 Documentation

- **[📘 User Guide](.doc/USER_GUIDE.md)** - How to use the features.
- **[🏗️ Architecture](.doc/ARCHITECTURE.md)** - System design and component diagrams.
- **[🧪 Testing Guide](.doc/TESTING.md)** - How to run and add tests.
- **[📝 Change Log](ChangeLog.md)** - Version history.

---

## ✨ Key Features

- **📄 Single-File Export (Ctrl+M)**: Export entire project hierarchies to unified Markdown files optimized for LLM consumption.
- **🔄 Git Integration (Ctrl+G)**: Generate AI-ready summaries of code changes and Git diffs.
- **🧪 Test Automation (Ctrl+T)**: Execute unit tests with progress tracking and EMA-based time estimation.
- **📂 Recent Files Management**: Organize and track generated artifacts with drag-drop support.
- **🔍 Smart Filtering**: Respect `.gitignore`, `.vtignore`, and [File Markers](.doc/GUIDE-FileMarkers-1.0.md).

---

## 🚀 Getting Started

### Prerequisites

- **Windows OS** (XP SP3 or later for WinForms)
- **.NET 8.0 Runtime** or SDK
- **Git** (for repository operations)

### Installation

```bash
# Clone repository
git clone https://github.com/your-repo/VecTool.git
cd VecTool

# Build & Run
dotnet restore
dotnet build -c Release
dotnet run --project OaiUI/oaiUI.csproj
```

See [User Guide](.doc/USER_GUIDE.md) for detailed configuration instructions.

---

## 🏗️ Architecture

VecTool follows **SOLID principles** and is split into modular layers:

- **Presentation**: `OaiUI` (WinForms)
- **Application**: `Handlers` (Logic)
- **Domain**: `Core` (Git, File System)
- **Infrastructure**: `Configuration`, `LogCtx`

See [Architecture Documentation](.doc/ARCHITECTURE.md) for detailed diagrams and design patterns.

---

## 🤝 Contributing

We welcome contributions! Please see the [Architecture Guide](.doc/ARCHITECTURE.md) and [Testing Guide](.doc/TESTING.md) to get started.

1. Fork the repository
2. Create feature branch
3. Submit Pull Request

---

## 📝 License

VecTool is licensed under the **Apache 2.0 License**. See [LICENSE](LICENSE-2.0.txt) file for details.
