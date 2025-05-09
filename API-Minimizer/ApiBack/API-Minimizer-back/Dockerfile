# Use official .NET runtime as the base image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use official .NET SDK for the build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["ApiBack/API-Minimizer-back/API-Minimizer-back.csproj", "ApiBack/API-Minimizer-back/"]
COPY ["MinimizerModel/MinimizerCommon/MinimizerCommon.csproj", "MinimizerModel/MinimizerCommon/"]
RUN dotnet restore "ApiBack/API-Minimizer-back/API-Minimizer-back.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/ApiBack/API-Minimizer-back"
RUN dotnet build "API-Minimizer-back.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "API-Minimizer-back.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: Use a non-root user for better security
FROM base AS final
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Create a non-root user and switch to it
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

# Set the entry point
ENTRYPOINT ["dotnet", "API-Minimizer-back.dll"]