
# Automate SerpApi dotnet library
#  compilation, test and release
#
.PHONY: test

name=serpapi
root=`pwd`
example=google

all: clean restore build test

# clean-up previous build
clean:
	rm -rf test/obj test/bin
	rm -rf serpapi/obj serpapi/bin
	dotnet clean

# rebuild local state
restore:
	dotnet restore

# build for all target framework defined in serpapi/serpapi.csproj
build:
	dotnet build --configuration Release

# run test regression
test:
	dotnet test --configuration Release

# run a simple application
run:
	dotnet run

# package the library
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
