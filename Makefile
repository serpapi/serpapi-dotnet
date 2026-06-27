
.PHONY: all clean restore build test pack

all: clean restore build test

clean:
	dotnet clean --verbosity quiet
	rm -rf serpapi/bin serpapi/obj test/bin test/obj

restore:
	dotnet restore

build:
	dotnet build --configuration Release --no-restore

test:
	dotnet test --configuration Release --no-build

pack:
	dotnet pack --configuration Release --no-build
