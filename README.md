# Sweng2k18-Publisher
Publisher Program for Socket Initialization

A program that operates as the server (publisher) side of a socket connection. The server's functionality is to send CSV files,
1 line at a time across a network stream to a client (subscriber) program. The server is multithreaded so that multiple subscribers
can access the CSV files on the server. The server utilizes a CSVReader to read in data from a CSV file.
