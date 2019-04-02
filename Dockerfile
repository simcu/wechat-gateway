FROM microsoft/dotnet:2.1-aspnetcore-runtime
COPY dist /home
WORKDIR /home
EXPOSE 5000
ENTRYPOINT ["dotnet", "Wechat.dll"]