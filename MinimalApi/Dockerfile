FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app
COPY . .

RUN dotnet restore "MinimalApi.csproj" \
    && dotnet publish "MinimalApi.csproj" -c Release -o /app/publish

EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "/app/publish/MinimalApi.dll"]