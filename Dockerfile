# Build sdk image
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./ParallelTracker/ParallelTracker.csproj ./ParallelTracker/
RUN dotnet restore ./ParallelTracker/ParallelTracker.csproj

# Copy everything else and build the project
COPY . .
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ConnectionStrings__ApplicationConnection=$ConnectionStrings__ApplicationConnection
ENV Google__ClientId=$Google__ClientId
ENV Google__ClientSecret=$Google__ClientSecret
ENV GitHub__ClientId=$GitHub__ClientId
ENV GitHub__ClientSecret=$GitHub__ClientSecret
ENV GitHubApi__ClientId=$GitHubApi__ClientId
ENV GitHubApi__ClientSecret=$GitHubApi__ClientSecret
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Run the app on container startup
#ENTRYPOINT [ "dotnet", "ParallelTracker.dll"]

CMD ASPNETCORE_URLS=http://*:$PORT dotnet ParallelTracker.dll
