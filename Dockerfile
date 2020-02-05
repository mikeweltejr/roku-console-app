FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app
USER root

# Copy csproj and restore as distinct layers
COPY ./roku-console-app.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./ ./
RUN dotnet publish ./roku-console-app.csproj -c Release -r linux-arm -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1.1-buster-slim-arm32v7
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "roku-console-app.dll"]
