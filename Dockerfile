FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Taskify/Taskify.csproj", "Taskify/"]
RUN dotnet restore "Taskify/Taskify.csproj"

COPY . .
WORKDIR "/src/Taskify"
RUN dotnet publish "Taskify.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Taskify.dll"]
