FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy toàn bộ solution
COPY . .

# Restore và build từ root
RUN dotnet restore "src/EMS.API/EMS.API.csproj"
RUN dotnet publish "src/EMS.API/EMS.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EMS.API.dll"]
