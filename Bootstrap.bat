@echo off

echo !!!!!!!!!!!!!!!!!!!!!!
echo !RUN AS ADMINISTRATOR!
echo !!!!!!!!!!!!!!!!!!!!!!
echo:

echo Bootstrapping TetrisTower project
echo:

pushd "%~dp0"

echo Checking for licensed project dependency...
echo:
if not exist ..\TetrisTower-LicensedAssets\ (
	git clone https://github.com/NibbleByte/TetrisTower-LicensedAssets.git ../TetrisTower-LicensedAssets
)

echo Linking dependency project to the main...
echo:

mklink /D Assets\ArtLicensed "..\..\TetrisTower-LicensedAssets\Assets\ArtLicensed"
mklink /D Assets\PluginsLicensed "..\..\TetrisTower-LicensedAssets\Assets\PluginsLicensed"
mklink /D "Packages\Bird Flocks" "..\..\TetrisTower-LicensedAssets\Packages\Bird Flocks"
echo:

pause