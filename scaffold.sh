#!/bin/bash
set -e

# Create solution
dotnet new sln -n QuanLyDangKyHocPhan

# Create projects
dotnet new avalonia.app -n QuanLyDKHP.App -o src/QuanLyDKHP.App --framework net8.0 --no-restore || true
dotnet new classlib -n QuanLyDKHP.Core -o src/QuanLyDKHP.Core --framework net8.0 --no-restore
dotnet new classlib -n QuanLyDKHP.Infrastructure -o src/QuanLyDKHP.Infrastructure --framework net8.0 --no-restore
dotnet new classlib -n QuanLyDKHP.Services -o src/QuanLyDKHP.Services --framework net8.0 --no-restore
dotnet new xunit -n QuanLyDKHP.Tests -o tests/QuanLyDKHP.Tests --framework net8.0 --no-restore

# Add to solution
dotnet sln add src/QuanLyDKHP.App/QuanLyDKHP.App.csproj
dotnet sln add src/QuanLyDKHP.Core/QuanLyDKHP.Core.csproj
dotnet sln add src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj
dotnet sln add src/QuanLyDKHP.Services/QuanLyDKHP.Services.csproj
dotnet sln add tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj

# Set references
dotnet add src/QuanLyDKHP.Services/QuanLyDKHP.Services.csproj reference src/QuanLyDKHP.Core/QuanLyDKHP.Core.csproj
dotnet add src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj reference src/QuanLyDKHP.Core/QuanLyDKHP.Core.csproj
dotnet add src/QuanLyDKHP.App/QuanLyDKHP.App.csproj reference src/QuanLyDKHP.Services/QuanLyDKHP.Services.csproj src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj src/QuanLyDKHP.Core/QuanLyDKHP.Core.csproj
dotnet add tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj reference src/QuanLyDKHP.Services/QuanLyDKHP.Services.csproj

# Add packages to App
dotnet add src/QuanLyDKHP.App/QuanLyDKHP.App.csproj package CommunityToolkit.Mvvm
dotnet add src/QuanLyDKHP.App/QuanLyDKHP.App.csproj package Microsoft.Extensions.DependencyInjection
dotnet add src/QuanLyDKHP.App/QuanLyDKHP.App.csproj package Microsoft.Extensions.Configuration.Json

# Add packages to Infrastructure
dotnet add src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj package ClosedXML
dotnet add src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj package QuestPDF

# Add packages to Services
dotnet add src/QuanLyDKHP.Services/QuanLyDKHP.Services.csproj package BCrypt.Net-Next

echo "Scaffolding completed!"
