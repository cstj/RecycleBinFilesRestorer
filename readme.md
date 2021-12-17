Recycle Bin Files Restorer
===============================

Really quick and dirty C# program for parsing $R and $I files from the recycle bin and recovery the path and filename of the original.
This is tested in a very narrow situation for my own use.

Has many bugs but the skeleton is there and it did what I needed.

Essentially it:
- Reads a directory that contains recycle bin files of the form $I* and $R*.  These files are index files (I) and data files (R).
- Matches up $I and $R file pairs
- Parses the $I files for their information.  These contain the original path, file size, date, header etc.
- Allows the user to select a directory and recovers them to there with the full path (replacing the : in the drive path with nothign so its not an illigal path.

