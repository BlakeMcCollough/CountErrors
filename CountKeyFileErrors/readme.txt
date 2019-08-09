Author: Blake McCollough
Contact: blakemccollough@yahoo.com
Description:
	CountKeyFileErrors.exe reads through all QS1 logs from a specified folder and internally counts =C2A008x0 where x is 0-F.
	The purpose of this is to identify any key file errors that may corrupt and file and crash the supervisor after a service pack update.
	The application takes a root folder, where it begins counting QS1 logs in every subdirectory. Ideally, the root folder should contain
	client directories (ie 0324, 0942, 0992, etc...) otherwise weird things happen. A start and end date may be provided to narrow down to
	files date that are of interest. Skip threshold is an integer that may be given (defaults to 10), if the amount of errors found is
	below this number, the entire file is omitted. When finished counting, output saved as Diagnosis.txt is created in the same path as the
	applicationtion. The output's format is as follows...
	
	clientID (name errorNo count)

	...where the section in parenthesis may be repeated for different errorNo. Each line will correspond to a clientID