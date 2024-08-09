import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

const config = JSON.parse(open('../config.json'));
const reqObject = {};
const productIdentifiersObj = {};
const productVersionsObj = {};
const sinceDateTimeObj = {};
const healthObj = {};
const newfilespublishedObj = {};
const unmactchedObj = {};
const reqByNameObj = {};

/**
 * @param {Object} logFile The Object of logfile 
 * @param {Number} Index Index to be searched by user
*/
export function getRequestDetailsFromLog(logFile, index) {
    let recordsCount = Object.keys(logFile).length;
    if (index > recordsCount) {
        console.error("File index out of bounds");
    }
    reqObject.url = logFile[index].Properties.Url;
    reqObject.requestMethod = logFile[index].Properties.requestMethod;
    reqObject.requestBodyText = logFile[index].Properties.requestBodyText;
    return reqObject;
}

export function filterRequestType(reqObject) {

    if (reqObject.url.includes('productIdentifiers')) {
        productIdentifiersObj.url = config.Base_URL + config.IdentifierEndpoint;
        productIdentifiersObj.requestBodyText = reqObject.requestBodyText;
        productIdentifiersObj.requestMethod = reqObject.requestMethod;
        return productIdentifiersObj;
    }

    else if (reqObject.url.includes('productVersions')) {
        productVersionsObj.url = config.Base_URL + config.VersionsEndpoint;
        productVersionsObj.requestBodyText = reqObject.requestBodyText;
        productVersionsObj.requestMethod = reqObject.requestMethod;
        return productVersionsObj;
    }

    else if (reqObject.url.includes('sinceDateTime')) {
        let date = GetNormalizedSinceDateTimeDate(reqObject.url);
        let sinceDateURI = encodeURI(config.Base_URL + config.DateTimeEndpoint + date);
        sinceDateTimeObj.url = sinceDateURI;
        sinceDateTimeObj.requestBodyText = {};
        sinceDateTimeObj.requestMethod = reqObject.requestMethod;
        return sinceDateTimeObj;
    }

    else if (reqObject.url.includes('health')) {
        healthObj.url = config.Base_URL + "/health";
        healthObj.requestBodyText = {};
        healthObj.requestMethod = reqObject.requestMethod;
        return healthObj;
    }

    else if (reqObject.url.includes('newfilespublished')) {
        newfilespublishedObj.url = config.Base_URL + config.FilesPublished;
        newfilespublishedObj.requestBodyText = reqObject.requestBodyText;
        newfilespublishedObj.requestMethod = reqObject.requestMethod;
        return newfilespublishedObj;
    }

    console.warn("Unmached Request type:" + reqObject.url)
    unmactchedObj.url = reqObject.url;
    unmactchedObj.requestBodyText = reqObject.requestBodyText;
    unmactchedObj.requestMethod = reqObject.requestMethod;
    return unmactchedObj;
}

export function filterByURLContent(reqObject, reqName) {
    if (reqObject.url.includes(reqName)) {
        reqByNameObj.url = config.Base_URL + reqName;
        reqByNameObj.requestBodyText = reqObject.requestBodyText;
        reqByNameObj.requestMethod = reqObject.requestMethod;
        return reqByNameObj;
    }
    return null;
}

export function filterByDate(reqObject, reqName) {
    if (reqObject.url.includes(reqName)) {
        let date = GetNormalizedSinceDateTimeDate(reqObject.url);
        let sinceDateURI = encodeURI(config.Base_URL + config.DateTimeEndpoint + date);
        sinceDateTimeObj.url = sinceDateURI;
        sinceDateTimeObj.requestBodyText = null;
        sinceDateTimeObj.requestMethod = reqObject.requestMethod;
        return sinceDateTimeObj;
    }
    return null;
}

export function getStartTime(timeStamp, index) {
    const deplayPerVU = new Map();
    const ts = Date.parse(timeStamp);
    const myTS = new Date(ts);
    var min = myTS.getUTCMinutes();
    var sec = myTS.getUTCSeconds();
    var ms = myTS.getUTCMilliseconds();
    var reqTimeInSec = ((min * 60) + sec + (ms / 1000));
    if (deplayPerVU.size == 1) {
        deplayPerVU.set(index, reqTimeInSec)
        console.warn("Delay Inserted at: " + index + " value:" + reqTimeInSec);
        return deplayPerVU.get(index);
    }

    else if (deplayPerVU.size == 2) {
        deplayPerVU.set(index, reqTimeInSec)
        console.warn("Delay Inserted at: " + index + " value:" + reqTimeInSec);
        return deplayPerVU.get(index);
    }
    else {
        deplayPerVU.set(index, reqTimeInSec);
        console.log("Thead:" + index, "Delay Time:" + deplayPerVU.get[index], "Last Req:" + deplayPerVU.get[index - 1]);
        return deplayPerVU.get(index);
    }

}

export function GetSinceDateTimeData() {
    const currentDateTime = new Date();
    currentDateTime.setDate(currentDateTime.getDate() - randomIntBetween(2, 20));
    return encodeURI(currentDateTime.toUTCString());
}

export function GetNormalizedSinceDateTimeDate(logDate) {
    let sinceDateText = logDate.substring(logDate.lastIndexOf('sinceDateTime=') + 14, logDate.lastIndexOf('GMT') + 3);
    sinceDateText = decodeURI(sinceDateText); //RFC 1123 format

    const sinceDate = parseRFC1123Date(sinceDateText); // Native method - Invalid date format

    let diffOnCurrentDate = Math.abs(Date.now() - sinceDate);
    const msPerDay = 24 * 60 * 60 * 1000;
    const differenceDays = Math.floor(diffOnCurrentDate / msPerDay);
    if (isNaN(differenceDays)) {
        return new Date().toUTCString();
    }
    else if (differenceDays > 21) {
        const currentTime = new Date();
        const newSinceDate = new Date(currentTime);
        let normalziedSinceDateTime = newSinceDate.setDate(currentTime.getDate() - randomIntBetween(2, 20));
        return new Date(normalziedSinceDateTime).toUTCString();
    }
    else {
        return new Date(sinceDate).toUTCString();
    }
}

function parseRFC1123Date(dateString) {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const parts = dateString.split(' ');
    const day = parseInt(parts[1], 10);
    const month = months.indexOf(parts[2]);
    const year = parseInt(parts[3], 10);
    const timeParts = parts[4].split(':');
    const hours = parseInt(timeParts[0], 10);
    const minutes = parseInt(timeParts[1], 10);
    const seconds = parseInt(timeParts[2], 10);

    return new Date(Date.UTC(year, month, day, hours, minutes, seconds));
}

export function SetDelayTime(DelayLogFile) { // Add check for matching Req. type
    let timeStampArr = [];
    let index = 0;
    let recordsCount = Object.keys(DelayLogFile).length;

    for (index = 0; index < recordsCount; index++) {
        if (index == recordsCount - 1) {
            timeStampArr[index] = 0;
        }

        else {

            let currentTimeStamp = DelayLogFile[index].Timestamp;
            let nextTimeStamp = DelayLogFile[index + 1].Timestamp;

            let timeCurrentReq = new Date(currentTimeStamp);
            let timeNextReq = new Date(nextTimeStamp);

            let delayTimeMillSec = (timeNextReq.getTime() - timeCurrentReq.getTime());
            let delayTimeReq = delayTimeMillSec / 1000;

            timeStampArr[index] = delayTimeReq;
        }
    }
    return timeStampArr;
}

export function printDelay(DelayLogFile) {
    let timeStampArr = [];
    let recordsCount = Object.keys(DelayLogFile).length;
    timeStampArr[0] = new Date(DelayLogFile[0].Timestamp).getSeconds();
    for (let index = 1; index < recordsCount - 2; index++) {
        let currentTimeStamp = new Date(DelayLogFile[index].Timestamp);
        let nextTimeStamp = new Date(DelayLogFile[index + 1].Timestamp);
        let diffTimeStamp = (nextTimeStamp.getTime() - currentTimeStamp.getTime());
        let delayInSec = parseFloat(diffTimeStamp) / 1000;
        timeStampArr[index] = delayInSec;
    };
    return timeStampArr;
}

export function debugDelay(DelayLogFile) {
    let timeStampArr = [];
    let recordsCount = Object.keys(DelayLogFile).length;
    console.log("Total Requests:" + recordsCount);
    let startMin = new Date(DelayLogFile[0].Timestamp).getUTCMinutes();
    let startSec = new Date(DelayLogFile[0].Timestamp).getSeconds();
    timeStampArr[0] = (startMin * 60) + startSec;
    for (let index = 1; index < recordsCount; index++) {
        let currentTimeStamp = new Date(DelayLogFile[index - 1].Timestamp);
        let nextTimeStamp = new Date(DelayLogFile[index].Timestamp);
        let diffTimeStamp = (nextTimeStamp.getTime() - currentTimeStamp.getTime());
        let delayInSec = parseFloat(diffTimeStamp) / 1000;
        timeStampArr[index] = delayInSec;
    };
    return timeStampArr;
}

export function requestRatePerMinute(logFile) {
    const countsPerMinute = {};

    logFile.forEach(log => {
        const timeStamp = new Date(log.Timestamp);
        const minute = timeStamp.getUTCMinutes();
        const hour = timeStamp.getUTCHours();
        const minKey = `${hour}:${minute}`;
        countsPerMinute[minKey] = (countsPerMinute[minKey] || 0) + 1;
    });
    return countsPerMinute;
}

export function replaceInJson(jsonObject, search, replacement) {
    if (typeof jsonObject === 'string') {
        return jsonObject.split(search).join(replacement);

    } else if (Array.isArray(jsonObject)) {
        return jsonObject.map(item => replaceInJson(item, search, replacement));

    } else if (typeof jsonObject === 'object' && jsonObject !== null) {
        const result = {};
        for (const [key, value] of Object.entries(jsonObject)) {
            result[key] = replaceInJson(value, search, replacement);
        }
        return result;
    } else {
        return jsonObject;
    }
}
