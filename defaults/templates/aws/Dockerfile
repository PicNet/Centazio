FROM public.ecr.aws/lambda/dotnet:9

WORKDIR /var/task

COPY "bin/Release/net9.0/publish"  .

CMD [ "{{it.FileName}}::{{it.AssemblyName}}.Aws.{{it.ClassName}}Handler::Handle" ]