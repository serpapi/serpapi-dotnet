
.PHONY: test

name=serpapi
root=`pwd`
example=google

# set default framework version
framework_version=net8.0

# all
all: clean restore build test

clean:
	dotnet clean serpapi/
	dotnet clean test/

restore:
	dotnet restore

# build for all target framework defined in serpapi/serpapi.csproj
build:
	dotnet build --configuration Release --no-restore 

# build for a single framework
build_uniq:
	dotnet build --configuration Release --no-restore --framework ${framework_version}

test:
	dotnet test test/ --configuration Release
# dotnet test test/ --configuration Release --filter GoogleProductTest.TestSearch  --logger "console;verbosity=detailed"

run:
	dotnet run

pack:
	dotnet pack

oobt: pack
	$(MAKE) run_oobt example=google
	$(MAKE) run_oobt example=bing

run_oobt:
	cd example/${example} ; \
	dotnet add package --package-directory ${root}/${name} ${name} ; \
	dotnet build ; \
	dotnet run

# Dotnet
#
# https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-the-dotnet-cli
#
# Package API
release: oobt
	open serpapi/bin/Debug
	open -a "Google\ Chrome" https://www.nuget.org/packages/manage/upload
