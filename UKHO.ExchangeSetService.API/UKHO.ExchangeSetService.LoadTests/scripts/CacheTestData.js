const testData = JSON.parse(open('../LiveLogs/SCS-ENC.json'));
const logParser = require('../helper/LogParser.js');

let jsonIndex = 0;
let cellNameArr = [];
let attributes = {};

export default function main() {
    let recordCount = 0;

    while (getNextENCUpdateRecord() != null) {
        recordCount = recordCount + 1;
    }
}

export function getSingleProductPayloadData(index) {
    if (index >= parseInt(testData.count)) { console.error("No records found at index:" + index) }
    else {
        console.warn("getSingleProductPayloadData-->")
        return getENCUpdateRecord(index)
    }
}

export function getENCUpdateRecord(index) {
    for (let i = jsonIndex; i < parseInt(testData.count); i++) {
        console.log(i)
    }
}

export function getNextENCUpdateRecord() {
    for (let i = jsonIndex; i < parseInt(testData.count); i++) {
        if (testData.entries[i].attributes[0].key == 'Agency' && getlastUsed(cellNameArr, testData.entries[i].attributes[0].key == 'CellName', 2) == false) {
            let testPayload = `{"data":{"batchId":"${testData.entries[i].batchId}","attributes":${JSON.stringify((testData.entries[i].attributes))},"businessUnit":"${testData.entries[i].businessUnit}","batchPublishedDate":"${testData.entries[i].batchPublishedDate}","files":${JSON.stringify((testData.entries[i].files))}}}`
            console.log("Container ID:" + testData.entries[i].batchId)
            attributes = testData.entries[i].attributes[0]
            cellNameArr.push(testData.entries[i].attributes[0].key == 'CellName')
            jsonIndex = i + 1;
            // console.log(JSON.stringify(testPayload))
            return testPayload;
        }
    }

    console.error("Not matching record found")
    return null;
}

function getlastUsed(cellNameArr, element, index) {
    if (index >= cellNameArr.length) {
        return false;
    }

    // else{
    const lastUsed = cellNameArr.slice(-index);
    console.log(...cellNameArr)
    return lastUsed.includes(element)
    // }
}

function generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

export function getNextRecord(fileName, index) {
    for (let i = index; index < fileName.length; i++) {
        let requestObj = logParser.getRequestDetailsFromLog(fileName, i)
        let filterData = logParser.filterByURLContent(requestObj, "/webhook/newfilespublished")
        if (filterData != null) {
            let requestBodyText = JSON.parse(filterData.requestBodyText);
            let requestData = requestBodyText.data;
            let batchid = requestData.batchId

            requestData.batchId = generateGUID();
            console.log("Container ID:" + batchid + "-->" + requestData.batchId)
            let reqBodyText = JSON.stringify(requestData)
            filterData.requestBodyText = `{"data":${reqBodyText}}`
            return filterData
        }
    }
}