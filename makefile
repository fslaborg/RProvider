FAKEPATH=./packages/FAKE/tools/
FAKE=${FAKEPATH}FAKE.exe

bin: ${FAKE}
	mono ${FAKE} build.fsx 
${FAKE}:
	mono .nuget/NuGet.exe install FAKE -outputDirectory packages -Version 2.1.440-alpha -ExcludeVersion
