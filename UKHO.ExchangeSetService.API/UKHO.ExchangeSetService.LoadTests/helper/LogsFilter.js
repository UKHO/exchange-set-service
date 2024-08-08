const fs = require('fs');
const path = require('path');

const logFilePath = path.join(__dirname, './Logs/Sample.json'); // Replace 'logs.json' with your actual file name
const searchString = 'Agency'; // Replace with the string you want to match

// Function to filter logs
function filterLogs(filePath, searchString) {
    // Read the file
    fs.readFile(filePath, 'utf8', (err, data) => {
        if (err) {
            console.error('Error reading file:', err);
            return;
        }

        try {
            // Parse the JSON data
            const logs = JSON.parse(data);

            // Filter logs based on the searchString
            const filteredLogs = logs.filter(logEntry => {
                // Adjust this condition based on your log structure
                return JSON.stringify(logEntry).includes(searchString);
            });

            // Convert the filtered logs back to JSON
            const updatedData = JSON.stringify(filteredLogs, null, 2); // Pretty print with 2-space indentation

            // Write the filtered logs back to the file
            fs.writeFile(filePath, updatedData, 'utf8', writeErr => {
                if (writeErr) {
                    console.error('Error writing file:', writeErr);
                } else {
                    console.log('File updated successfully.');
                }
            });
        } catch (parseErr) {
            console.error('Error parsing JSON:', parseErr);
        }
    });
}

// Call the function
filterLogs(logFilePath, searchString);