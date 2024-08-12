const logParser = require('../Helper/LogParser.js');
function generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

export function getNextNewFilePublishedRecord(logFileName, index) {
    for (let i = index; index < logFileName.length; i++) {
        let requestObj = logParser.getRequestDetailsFromLog(logFileName, i);
        let filterData = logParser.filterByURLContent(requestObj, "/webhook/newfilespublished");
        if (filterData != null) {
            let requestBodyText = JSON.parse(filterData.requestBodyText);
            let requestData = requestBodyText.data;
            let batchid = requestData.batchId;

            requestData.batchId = generateGUID();
            console.log("Container ID:" + batchid + "-->" + requestData.batchId);
            let reqBodyText = JSON.stringify(requestData);
            filterData.requestBodyText = `{"data":${reqBodyText}}`;
            return filterData;
        }
    }
}