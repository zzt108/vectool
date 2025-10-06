# VecTool

**VecTool** is a lightweight developer utility for managing project exports, Git workflow automation, and test execution. Designed for .NET developers who want quick access to common tasks without API dependencies.

---

## 🚀 Current Features (v4.25.1007)

### Core Functionality (No API Keys Required)

- **📄 Markdown Export** – Export entire project structure as a single `.md` file
- **🔧 Git Changes Integration** – Generate AI-assisted commit messages from staged changes
- **✅ Run Unit Tests** – Execute `dotnet test` with `Ctrl+T` keyboard shortcut
- **📂 Recent Files System** – Drag-drop support, filtering, automatic cleanup

---

## 🏗️ Architecture

VecTool is modularized into **7 projects**:

| Project | Responsibility |
|---------|---------------|
| **Vectool.UI** | Main WinForms user interface layer |
| **Configuration** | app.config abstraction, settings stores |
| **Constants** | Centralized tag names and XML attributes |
| **Core** | Business logic (Git ops, file system traversal) |
| **Handlers** | Feature handlers (Markdown export, Git changes, test runner) |
| **RecentFiles** | Recent Files manager with drag-drop support |
| **Utils** | Utility classes (MIME detection, file helpers) |

---

## 📦 Dependencies

- **NLog** – Structured logging
- **Serilog** – Alternative logging backend
- **OpenAI-DotNet** (v8.4.1) – For Git commit message generation
- **LogCtx** (Git submodule) – Contextual logging utilities

---

## 🔨 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git (for submodule initialization)

### Build Instructions

