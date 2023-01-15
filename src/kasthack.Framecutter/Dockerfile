#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "kasthack.Framecutter/"]
COPY ["kasthack.Framecutter/kasthack.Framecutter.csproj", "kasthack.Framecutter/"]
RUN dotnet restore "kasthack.Framecutter/kasthack.Framecutter.csproj"
COPY . .
WORKDIR "/src/kasthack.Framecutter"
RUN dotnet build "kasthack.Framecutter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "kasthack.Framecutter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN apt update && apt install -yf libgdiplus \
	&& rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "kasthack.Framecutter.dll"]