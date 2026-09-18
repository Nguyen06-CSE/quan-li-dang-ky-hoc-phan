#!/bin/bash
set -e

# App packages
dotnet add src/QuanLyDKHP.App/QuanLyDKHP.App.csproj package CommunityToolkit.Mvvm -v 8.4.0
dotnet add src/QuanLyDKHP.App/QuanLyDKHP.App.csproj package Microsoft.Extensions.DependencyInjection -v 8.0.0
dotnet add src/QuanLyDKHP.App/QuanLyDKHP.App.csproj package Microsoft.Extensions.Configuration.Json -v 8.0.0

# Infrastructure packages
dotnet add src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL -v 8.0.4
dotnet add src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj package ClosedXML -v 0.102.3
dotnet add src/QuanLyDKHP.Infrastructure/QuanLyDKHP.Infrastructure.csproj package QuestPDF -v 2024.3.4

# Services packages
dotnet add src/QuanLyDKHP.Services/QuanLyDKHP.Services.csproj package BCrypt.Net-Next -v 4.0.3

# Test packages
dotnet add tests/QuanLyDKHP.Tests/QuanLyDKHP.Tests.csproj package Moq -v 4.20.70 || true

echo "Packages fixed!"
