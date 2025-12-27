# DMS Migration Platform

A comprehensive Document Management System (DMS) migration platform built with ASP.NET Core and PostgreSQL. This enterprise-grade application provides secure, multi-tenant data migration capabilities with support for SharePoint, file systems, and other document sources.

## 🚀 Features

### Core Functionality

- **Multi-Tenant Architecture** - Isolated tenant data with claims-based context
- **Connection Management** - Configure and manage multiple data source connections
- **Discovery Scanner** - Automated scanning and analysis of document repositories
- **Job Queue System** - Background job processing for large-scale migrations
- **SharePoint Integration** - Native SharePoint client for seamless data extraction
- **Audit Trail** - Comprehensive logging of all system activities

### Security & Authentication

- **Cookie-Based Authentication** - Secure session management
- **Role-Based Access Control (RBAC)** - Admin, Operator, and Viewer roles
- **Password Hashing** - Industry-standard ASP.NET Core Identity PasswordHasher
- **Anti-Forgery Protection** - CSRF token validation
- **Data Protection** - Encrypted sensitive data storage

### User Experience

- **Modern UI** - Clean, responsive interface
- **Toast Notifications** - Real-time user feedback
- **Health Monitoring** - Built-in health checks for database connectivity
- **Development Seeding** - Pre-configured test users for development

## 🏗️ Architecture

This application follows **Clean Architecture** principles with clear separation of concerns:

```
src/
├── Migration.Domain/          # Enterprise business rules and entities
│   ├── Entities/             # Domain models (Connection, Tenant, User, etc.)
│   └── Enums/                # Domain enumerations
│
├── Migration.Application/     # Application business rules
│   ├── Connections/          # Connection commands, queries, DTOs
│   ├── Discovery/            # Discovery scanning logic
│   ├── Jobs/                 # Background job interfaces
│   └── Interfaces/           # Service contracts
│
├── Migration.Infrastructure/  # External concerns & implementation
│   ├── Data/                 # EF Core DbContext
│   ├── Persistence/          # Database repositories
│   ├── Services/             # Service implementations
│   └── SharePoint/           # SharePoint client integration
│
└── Migration.Web/            # ASP.NET Core MVC presentation
    ├── Controllers/          # MVC controllers
    ├── Views/               # Razor views
    ├── ViewModels/          # View-specific models
    └── wwwroot/             # Static assets
```

## 📋 Prerequisites

- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **PostgreSQL 12+** - [Download](https://www.postgresql.org/download/)
- **Visual Studio 2022** or **VS Code** (optional)
- **Git** - [Download](https://git-scm.com/downloads)

## 🔧 Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/DMS.Migration.git
cd DMS.Migration
```

### 2. Database Setup

Create a PostgreSQL database and run the setup scripts:

```bash
# Create database
psql -U postgres -c "CREATE DATABASE DMS;"

# Run discovery schema
psql -U postgres -d DMS -f database-discovery.sql

# Run authentication setup
psql -U postgres -d DMS -f database-auth-setup.sql

# Run main database setup
psql -U postgres -d DMS -f database-setup.sql
```

### 3. Configure Connection String

Update `src/Migration.Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=DMS;Username=postgres;Password=yourpassword"
  }
}
```

### 4. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project src/Migration.Web
```

The application will be available at **http://localhost:5000**

## 🔑 Default Credentials

Development environment includes pre-seeded users:

| Role     | Email              | Password     | Description          |
| -------- | ------------------ | ------------ | -------------------- |
| Admin    | admin@dms.local    | Admin@123    | Full system access   |
| Operator | operator@dms.local | Operator@123 | Migration operations |

⚠️ **Important:** Change these credentials in production environments!

## 📚 Documentation

- [Authentication Implementation Guide](AUTHENTICATION_IMPLEMENTATION.md) - Complete authentication system documentation
- [Testing Guide](TESTING_GUIDE.md) - Testing scenarios and validation steps

## 🗂️ Database Schema

### Core Tables

- **Tenants** - Multi-tenant organization data
- **Users** - System users with roles and credentials
- **Connections** - Data source connection configurations
- **ConnectionSecrets** - Encrypted connection credentials
- **DiscoveryRuns** - Discovery scan execution records
- **DiscoveryItems** - Scanned documents and metadata
- **AuditEvents** - System activity audit trail
- **Jobs** - Background job queue

## 🛠️ Development

### Running Tests

```bash
dotnet test
```

### Database Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/Migration.Infrastructure --startup-project src/Migration.Web

# Update database
dotnet ef database update --project src/Migration.Infrastructure --startup-project src/Migration.Web
```

### Build for Production

```bash
dotnet publish -c Release -o ./publish
```

## 🔐 Security Considerations

- All passwords are hashed using ASP.NET Core Identity's `PasswordHasher`
- Connection secrets are encrypted at rest
- Anti-forgery tokens protect against CSRF attacks
- Session cookies are HttpOnly and use secure policies
- SQL injection protection via Entity Framework Core parameterized queries
- Role-based authorization on all protected endpoints

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Your Name** - _Initial work_ - [YourGitHub](https://github.com/yourusername)

## 🙏 Acknowledgments

- ASP.NET Core team for the excellent framework
- Entity Framework Core for robust ORM capabilities
- PostgreSQL community for the reliable database system

## 📞 Support

For support, email support@dms.local or open an issue in the GitHub repository.

---

**Built with ❤️ using ASP.NET Core 8.0**
