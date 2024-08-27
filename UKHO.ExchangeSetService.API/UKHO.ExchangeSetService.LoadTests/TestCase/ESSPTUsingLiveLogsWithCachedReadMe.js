import { sleep } from 'k6';
import { scenario } from 'k6/execution';
import http from 'k6/http';
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { getCurrentStageIndex } from 'https://jslib.k6.io/k6-utils/1.3.0/index.js';

const essLogsFile = JSON.parse(open('../TestData/essLogs.json')); //Add path of Json file containing Azure logs of ESS (peak load hour) 
const essAPI = require('../Scripts/ReplayRequest.js');
const logParser = require('../Helper/LogParser.js');
const testHelper = require('../Helper/LoadStages.js');
const loadProfile=JSON.parse(open('../LoadProfile.json'));
const loadProfileVus=JSON.parse(open('../LoadProfileVus.json'));
const config=JSON.parse(open('../config.json'));
const readMeBody=JSON.parse(open('../TestData/invalidateReadMe.json'));
const stageTime = '1m';
const invalidateReadMeAfter='10m';

export const options = {
    scenarios: {
      'ESSPT_LiveLoad':{
        executor: 'ramping-arrival-rate',
        preAllocatedVUs: 180,
        timeUnit: '1m',
          stages: [
            { duration: '1m', target: 1 },
            { duration: '1m', target: 1 },
            { duration: '1m', target: 0 }, 
            { duration: '1m', target: 0 },
            { duration: '1m', target: 0 },
            { duration: '1m', target: 1 },
            { duration: '1m', target: 166 },
            { duration: '1m', target: 128 },
            { duration: '1m', target: 175 },
            { duration: '1m', target: 110 },
      
            { duration: '1m', target: 166 },
            { duration: '1m', target: 127 },
            { duration: '1m', target: 174 },
            { duration: '1m', target: 111 },
            { duration: '1m', target: 81 },
            { duration: '1m', target: 99 },
            { duration: '1m', target: 180 },
            { duration: '1m', target: 94 },
            { duration: '1m', target: 101 },
            { duration: '1m', target: 139 },
      
            { duration: '1m', target: 110 },
            { duration: '1m', target: 115 },
            { duration: '1m', target: 99 },
            { duration: '1m', target: 176 },
            { duration: '1m', target: 138 },
            { duration: '1m', target: 78 },
            { duration: '1m', target: 140 },
            { duration: '1m', target: 137 },
            { duration: '1m', target: 158 },
            { duration: '1m', target: 87 },
      
            { duration: '1m', target: 93 },
            { duration: '1m', target: 79 },
            { duration: '1m', target: 22 },
            { duration: '1m', target: 93 },
            { duration: '1m', target: 136 },
            { duration: '1m', target: 58 },
            { duration: '1m', target: 153 },
            { duration: '1m', target: 118 },
            { duration: '1m', target: 52 },
            { duration: '1m', target: 89 },
      
            { duration: '1m', target: 24 },
            { duration: '1m', target: 138 },
            { duration: '1m', target: 151 },
            { duration: '1m', target: 84 },
            { duration: '1m', target: 87 },
            { duration: '1m', target: 125 },
            { duration: '1m', target: 84 },
            { duration: '1m', target: 32 },
            { duration: '1m', target: 68 },
            { duration: '1m', target: 125 },
      
            { duration: '1m', target: 145 },
            { duration: '1m', target: 95 },
            { duration: '1m', target: 150 },
            { duration: '1m', target: 163 },
            { duration: '1m', target: 140 },
            { duration: '1m', target: 123 },
            { duration: '1m', target: 118 },
            { duration: '1m', target: 184 },
            { duration: '1m', target: 100 },
            { duration: '1m', target: 0 }
          ]
      },
      'invalidate-ReadMe':{
        exec:'invalidateReadMeCache',
        executor: 'shared-iterations',
        vus: 1,
        iterations: 1,
        maxDuration: '1m',
        startTime:invalidateReadMeAfter,
      }
    },
}

export function setup() {
  const testStartTime = new Date();
  console.warn("Test Start Time:" + testStartTime.toLocaleTimeString('en-US'));
  console.warn("Test Start Time(UTC):" + testStartTime.toUTCString());
  let requestDelayArr = logParser.getDelayFromTimeStamp(essLogsFile);
  return requestDelayArr;
}

export function teardown() {
  const testEndTime = new Date();
  console.warn("Test End Time:" + testEndTime.toLocaleTimeString('en-US'));
  console.warn("Test End Time(UTC):" + testEndTime.toUTCString());
}

export default function testRunner(requestDelayArr) {
    logIterator(requestDelayArr);
}

export function invalidateReadMeCache(){
  let req={};
    req.requestMethod='POST'
    req.url=config.Base_URL+config.FilesPublished;
    req.requestBodyText=JSON.stringify(readMeBody);

    console.log("URL:"+req.url,"Body:"+req.requestBodyText);
    // essAPI.replayRequest(req);
}

export function logIterator(requestDelayArr) {
    let selectLoadProfile = logParser.getRequestDetailsFromLog(essLogsFile, scenario.iterationInTest);
    let reqData = logParser.filterRequestType(selectLoadProfile);
    let delay = requestDelayArr[scenario.iterationInTest].toFixed(2);
    sleep(delay);
    console.log("||Stage:"+getCurrentStageIndex(),"||Delay:"+delay,"||Request:"+reqData.url)
      // essAPI.replayRequest(reqData);
    sleep(stageTime);
}

export function handleSummary(data) {
  return {
    ["ExecutionSummary/ReadMe-Vus-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["ExecutionSummary/ReadMe-Vus" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}