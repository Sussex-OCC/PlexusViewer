#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat/

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Sussex.Lhcra.Plexus.Viewer.UI/Sussex.Lhcra.Plexus.Viewer.UI.csproj", "Sussex.Lhcra.Plexus.Viewer.UI/"]
COPY ["Sussex.Lhcra.Plexus.Viewer.DataServices/Sussex.Lhcra.Plexus.Viewer.DataServices.csproj", "Sussex.Lhcra.Plexus.Viewer.DataServices/"]
COPY ["Sussex.Lhcra.Plexus.Viewer.Domain/Sussex.Lhcra.Plexus.Viewer.Domain.csproj", "Sussex.Lhcra.Plexus.Viewer.Domain/"]
COPY Nuget.Config ./
RUN dotnet restore "Sussex.Lhcra.Plexus.Viewer.UI/Sussex.Lhcra.Plexus.Viewer.UI.csproj" --configfile  Nuget.Config
COPY . .
WORKDIR "/src/Sussex.Lhcra.Plexus.Viewer.UI"
RUN dotnet build "Sussex.Lhcra.Plexus.Viewer.UI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sussex.Lhcra.Plexus.Viewer.UI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sussex.Lhcra.Plexus.Viewer.UI.dll"]