import { sleep } from 'k6';
import { scenario } from 'k6/execution';
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";

const essLogsFile = JSON.parse(open('../TestData/essLogs.json')); //Add path of Json file containing Azure logs of ESS (peak load hour); 
const essAPI = require('../Scripts/ReplayRequest.js');
const logParser = require('../Helper/LogParser.js');
const testHelper = require('../Helper/LoadStages.js');
const testStages = testHelper.getLoadStages(essLogsFile);
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
                { duration: stageTime, target: testStages[0] },
                { duration: stageTime, target: testStages[1] },
                { duration: stageTime, target: 0 },
                { duration: stageTime, target: 0 },
                { duration: stageTime, target: 0 },
                { duration: stageTime, target: testStages[2] },
                { duration: stageTime, target: testStages[3] },
                { duration: stageTime, target: testStages[4] },
                { duration: stageTime, target: testStages[5] },
                { duration: stageTime, target: testStages[6] },

                { duration: stageTime, target: testStages[7] },
                { duration: stageTime, target: testStages[8] },
                { duration: stageTime, target: testStages[9] },
                { duration: stageTime, target: testStages[10] },
                { duration: stageTime, target: testStages[11] },
                { duration: stageTime, target: testStages[12] },
                { duration: stageTime, target: testStages[13] },
                { duration: stageTime, target: testStages[14] },
                { duration: stageTime, target: testStages[15] },
                { duration: stageTime, target: testStages[16] },

                { duration: stageTime, target: testStages[17] },
                { duration: stageTime, target: testStages[18] },
                { duration: stageTime, target: testStages[19] },
                { duration: stageTime, target: testStages[20] },
                { duration: stageTime, target: testStages[21] },
                { duration: stageTime, target: testStages[22] },
                { duration: stageTime, target: testStages[23] },
                { duration: stageTime, target: testStages[24] },
                { duration: stageTime, target: testStages[25] },
                { duration: stageTime, target: testStages[26] },

                { duration: stageTime, target: testStages[27] },
                { duration: stageTime, target: testStages[28] },
                { duration: stageTime, target: testStages[29] },
                { duration: stageTime, target: testStages[30] },
                { duration: stageTime, target: testStages[31] },
                { duration: stageTime, target: testStages[32] },
                { duration: stageTime, target: testStages[33] },
                { duration: stageTime, target: testStages[34] },
                { duration: stageTime, target: testStages[35] },
                { duration: stageTime, target: testStages[36] },

                { duration: stageTime, target: testStages[37] },
                { duration: stageTime, target: testStages[38] },
                { duration: stageTime, target: testStages[39] },
                { duration: stageTime, target: testStages[40] },
                { duration: stageTime, target: testStages[41] },
                { duration: stageTime, target: testStages[42] },
                { duration: stageTime, target: testStages[43] },
                { duration: stageTime, target: testStages[44] },
                { duration: stageTime, target: testStages[45] },
                { duration: stageTime, target: testStages[46] },

                { duration: stageTime, target: testStages[47] },
                { duration: stageTime, target: testStages[48] },
                { duration: stageTime, target: testStages[49] },
                { duration: stageTime, target: testStages[50] },
                { duration: stageTime, target: testStages[51] },
                { duration: stageTime, target: testStages[52] },
                { duration: stageTime, target: testStages[53] },
                { duration: stageTime, target: testStages[54] },
                { duration: stageTime, target: testStages[55] }
            ],
      },
      'invalidate-ReadMe':{
        exec:'invalidateReadMeCache',
        executor: 'per-vu-iterations',
        vus: 1,
        iterations: 1,
        maxDuration: '1m',
        startTime:invalidateReadMeAfter
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

    essAPI.replayRequest(req);
}

export function logIterator(requestDelayArr) {
    let selectLoadProfile = logParser.getRequestDetailsFromLog(essLogsFile, scenario.iterationInTest);
    let reqData = logParser.filterRequestType(selectLoadProfile);
    let delay = requestDelayArr[scenario.iterationInTest].toFixed(2);
    sleep(delay);
       essAPI.replayRequest(reqData);
    sleep(stageTime);
}

export function handleSummary(data) {
  return {
      ["ExecutionSummary/ESSPTUsingLiveLogsWithCachedReadMe-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
      ["ExecutionSummary/ESSPTUsingLiveLogsWithCachedReadMe-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}