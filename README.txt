###Log Viewer

The LogViewer is an application that can open json-formatted log files and display the entries in a data grid for easy consumption, filtering and sorting. 

## Opening Large Files

For logs greater than 100MB it would be more efficient to open the log as a stream. This option is the "File" menu dropdown.
Opening a log as a stream literally returns a FileStream of the log wrapped around a cache. Giving near constant-time access to the pages of the log.

## Filtering

Log entries can be filtered based on the contents in the columns

