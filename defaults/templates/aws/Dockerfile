FROM public.ecr.aws/lambda/dotnet:10

WORKDIR /var/task

COPY "bin/Release/net10.0/publish"  .

CMD [ "{{it.FileName}}::{{it.AssemblyName}}.Aws.{{it.ClassName}}Handler::Handle" ]