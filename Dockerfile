FROM microsoft/dotnet:2.1-sdk as builder
WORKDIR /home
COPY . /home
RUN dotnet publish -c release -o dist

FROM microsoft/dotnet:2.1-aspnetcore-runtime
COPY --from=builder /home/dist /home
WORKDIR /home
EXPOSE 80 50051
ENTRYPOINT ["dotnet", "Wechat.dll"]