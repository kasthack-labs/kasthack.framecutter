#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Framecutter/Framecutter.csproj", "Framecutter/"]
RUN dotnet restore "Framecutter/Framecutter.csproj"
COPY . .
WORKDIR "/src/Framecutter"
RUN dotnet build "Framecutter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Framecutter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN apt update && apt install -yf libgdiplus \
	&& rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Framecutter.dll"]