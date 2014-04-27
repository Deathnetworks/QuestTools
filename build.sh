#!/bin/bash

NAME=$2
BASE=/c/db/Projects

echo $1 Build of $NAME from $BASE

PLUGINDIR=$BASE/$NAME
PLUGINFILE=$PLUGINDIR/$NAME.cs
BUILDHOME=$BASE/Builds/$NAME
ARCHIVE=$BASE/Archive

RUNDIR=$BASE/../Plugins/
SVNDIR=$BASE/../svn/$NAME/trunk/$NAME

#remove contents of output directories
rm -vrf $BUILDHOME
rm -vrf $RUNDIR/$NAME
rm -vrf $PLUGINDIR/bin
rm -vrf $PLUGINDIR/obj

# re-create output directory
mkdir -pv $BUILDHOME
mkdir -pv $ARCHIVE
mkdir -pv $SVNDIR

# copy plugin files to output directory
echo Executing rsync Copy
rsync -avm \
 --include='*.cs' \
 --include='*.xml' \
 --include='*.xaml' \
 --include='*.txt' \
 --include='*.dll' \
 -f 'hide,! */' $PLUGINDIR $BASE/BUILDS

echo Removing $BUILDHOME/bin
rm -vrf $BUILDHOME/bin
echo Removing $BUILDHOME/bin
rm -vrf $BUILDHOME/obj

if [[ "$1" == "Release" ]]
then
 RELEASE=true
else
 RELEASE=false
fi

if [[ "$RELEASE" = true ]] 
then

	#clean SVN
	rm -vrf $SVNDIR

	# cd to next level from output directory
	cd $BUILDHOME/..
	pwd

	echo Getting version from $PLUGINFILE

	if [[ $(egrep 'new Version' $PLUGINFILE | egrep '[\{\}]+') -gt 0 ]]
	then
	  echo 'ERROR: INVALID VERSION LINE FORMAT (found { or } characters)'
	  exit 1
	fi 

	PLUGINVERSION=`cat $PLUGINFILE | grep "new Version" | sed -r 's/^\s+//g' | sed -r 's/[a-zA-Z=\{\}(),;]+//g'`
	echo $PLUGINVERSION
	MAJOR=`echo $PLUGINVERSION | cut -d ' ' -f 1`
	MINOR=`echo $PLUGINVERSION | cut -d ' ' -f 2`
	OLDBUILD=`echo $PLUGINVERSION | cut -d ' ' -f 3`
	echo Found version Major=$MAJOR Minor=$MINOR Build=$OLDBUILD in $PLUGINFILE

	NEWBUILD=$[OLDBUILD+1]

	echo new build is return new Version\($MAJOR, $MINOR, $NEWBUILD\);
	VERSIONLINE="        public static Version PluginVersion = new Version($MAJOR, $MINOR, $NEWBUILD);"
	cat $PLUGINFILE | sed -e "s/.*public static Version PluginVersion.*/$VERSIONLINE/g" > $PLUGINFILE

	ZIPFILE=$NAME-$MAJOR.$MINOR.$NEWBUILD.zip

	echo using $ZIPFILE as filename

	ZCMD="7z a -y $ZIPFILE $NAME"
	echo running: $ZCMD
	$ZCMD

	echo copying Zip to Archive
	cp -v $ZIPFILE $ARCHIVE

	rm -rvf $SVNDIR
	cp -rv $BUILDHOME $SVNDIR/
fi

mkdir -pv $RUNDIR
cp -rv $BUILDHOME $RUNDIR/
