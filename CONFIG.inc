source ../CONFIG.inc

PACKAGE=Historian
TARGETDIR="$PACKAGE"
TARGETBINDIR="$TARGETDIR/"
PROJECTSDIR="$Source/"

VERSION=$( cat $PACKAGE.version | tr '\n' ' ' | sed -n -E 's/^.*?"VERSION\":\{.*"MAJOR":([0-9]+?),.*?"MINOR":([0-9]+?),.*?"PATCH":([0-9]+?),.*?"BUILD":([0-9]+?).*?\}.*$/\1.\2.\3.\4/p' )