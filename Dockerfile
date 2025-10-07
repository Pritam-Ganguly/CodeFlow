FROM mcr.mircosoft.com/dotnet/aspnet:9.0 AS build
WORKDIR /src

COPY ["CodeFlow.Web/CodeFlow.Web.csproj", "CodeFlow.Web/"]
COPY ["CodeFlow.Core/CodeFlow.Core.csproj", "CodeFlow.Core/"]
RUN dotnet restore "CodeFlow.Web/CodeFlow.Web.csproj"

COPY . .
WORKDIR "/src/CodeFlow.Web"
RUN dotnet build "CodeFlow.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeFlow.Web.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT [ "dotnet", "CodeFlow.Web.dll" ]
