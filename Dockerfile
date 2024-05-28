#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apt-get update
RUN apt-get remove krb5-config krb5-user
RUN apt install -y krb5-config 
RUN apt-get install -y krb5-user
VOLUME ["/krb5","/var/scratch"]

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MI.DEGProcessor/MI.DEGProcessor.csproj", "MI.DEGProcessor/"]
COPY ["external/IGS.DataServices/IGS.Models/IGS.Models.csproj", "IGS.Models/"]
COPY ["external/MI.Common/MI.Common/MI.Common.csproj", "MI.Common/"]
COPY ["external/ML.DataServices/ML.Models/ML.Models.csproj", "ML.Models/"]
COPY ["external/MI.Common/NLog.MSMQ.Target.Core/NLog.MSMQ.Target.Core.csproj", "NLog.MSMQ.Target.Core/"]
COPY ["external/IGS.DataServices/IGS.DataServices/IGS.DataServices.csproj", "IGS.DataServices/"]
COPY ["external/MI.Common/ML.Common/ML.Common.csproj", "ML.Common/"]
COPY ["external/ML.DataServices/ML.DataServices/ML.DataServices.csproj", "ML.DataServices/"]
RUN dotnet restore "MI.DEGProcessor/MI.DEGProcessor.csproj"
COPY . .
WORKDIR "/src/MI.DEGProcessor"
RUN dotnet build "MI.DEGProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MI.DEGProcessor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
WORKDIR /
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
RUN apt-get update
RUN apt install -y krb5-config 
RUN apt-get install -y krb5-user
WORKDIR /app
ENTRYPOINT ["dotnet", "MI.DEGProcessor.dll"]