Author: Blake McCollough
Contact: blakemccollough@yahoo.com
Description:
	CountKeyFileErrors.exe reads through all QS1 logs from a specified folder and internally counts =C2A00xx where xx is 00-FF.
	The purpose of this is to more broadly show what errors are happening to all the customers, not just specific keyfile errors.
	The application takes a root folder, where it begins counting QS1 logs in every subdirectory. Ideally, the root folder should contain
	client directories (ie 0324, 0942, 0992, etc...) otherwise weird things happen. A start and end date may be provided to narrow down to
	files date that are of interest. When finished counting, output is displayed on screen as a list of discovered errors (and the total
	they are counted). When a list item is clicked, customers in which the specific error is found are displayed. This display can
	be stored as a .txt output using the export button. The output's format is as follows...
	
	{ Error = error (totalErrorCount), Client = clientID, Count = count, Total = (totalErrorCount) }

	...where each line will correspond to a clientID & error combination.