# TechSmartAggregator
Consolidate quizzes and assignments from TechSmart gradebook

Usage: TechSmartAggregator <file.csv>

New aggreated CSV file is output to Output subdirectory.

Completes the following batch tasks:
* Removes empty columns
* Aggregates all Classwork assignments by lesson number
* Aggregates all Homework assignments by lesson number
* Cleans up all Assessments to show only score
* Preserves total points possible in header

What results is a much cleaner grade book that's easily entered in Synergy
