FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.3-buster-slim
WORKDIR /app
COPY GkeCloudflareSync/bin/Release/netcoreapp3.1/publish/ .
EXPOSE 80
ENTRYPOINT ["dotnet", "GkeCloudflareSync.dll"]
