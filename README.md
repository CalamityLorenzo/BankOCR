# BankOCR

The service is the OCRProcessor in BanOCK->OcrProcessor.
I wrote this from the top down completing in user story in turn, and then making adjustments as I went along.
As a result it works like a workflow, where you can pick and choose which feature you want to use.
As it's all string manipulations, I went in with the idea of reducing allocations use valuetypes where appropriate and one place I used a space to create string. 
I didn't get as far as much as I would have liked with this, but that was more about optimising rather than getting the task done.

Tests are split in to two sections UnitTests that I used to implement the OCRProcessor and the UserStories themselves.
The text files in the test project are the inputs for stories 1,3, and 4. Story 2 inputs were supplied as parameters.

