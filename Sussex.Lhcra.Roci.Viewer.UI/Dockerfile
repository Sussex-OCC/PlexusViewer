#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-nanoserver-1809 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1809 AS build
WORKDIR /src
COPY ["Sussex.Lhcra.Roci.Viewer.UI/Sussex.Lhcra.Roci.Viewer.UI.csproj", "Sussex.Lhcra.Roci.Viewer.UI/"]
COPY ["Sussex.Lhcra.Roci.Viewer.DataServices/Sussex.Lhcra.Roci.Viewer.DataServices.csproj", "Sussex.Lhcra.Roci.Viewer.DataServices/"]
COPY ["Sussex.Lhcra.Roci.Viewer.Domain/Sussex.Lhcra.Roci.Viewer.Domain.csproj", "Sussex.Lhcra.Roci.Viewer.Domain/"]
RUN dotnet restore "Sussex.Lhcra.Roci.Viewer.UI/Sussex.Lhcra.Roci.Viewer.UI.csproj"
COPY . .
WORKDIR "/src/Sussex.Lhcra.Roci.Viewer.UI"
RUN dotnet build "Sussex.Lhcra.Roci.Viewer.UI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sussex.Lhcra.Roci.Viewer.UI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sussex.Lhcra.Roci.Viewer.UI.dll"]