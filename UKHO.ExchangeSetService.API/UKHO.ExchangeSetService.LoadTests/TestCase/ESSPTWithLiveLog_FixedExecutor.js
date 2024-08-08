import { sleep } from 'k6';
import { scenario } from 'k6/execution';
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";

const loadProfile = JSON.parse(open('../LiveLogs/filtered.json'));
const essAPI = require('../scripts/ReplayRequest.js');
const logParser = require('../helper/LogParser.js');
const testHelper = require('../helper/LoadStages.js');
const testStages = testHelper.getLoadStages(loadProfile);
const StageTime = '1m'

export const options = {
  scenarios: {
    'hardcoded-executor': {
      executor: 'ramping-arrival-rate',
      preAllocatedVUs: 180,
      timeUnit: '1m',
      stages: [
        { duration: StageTime, target: testStages[0] },
        { duration: StageTime, target: testStages[1] },
        { duration: StageTime, target: 0 },
        { duration: StageTime, target: 0 },
        { duration: StageTime, target: 0 },
        { duration: StageTime, target: testStages[2] },
        { duration: StageTime, target: testStages[3] },
        { duration: StageTime, target: testStages[4] },
        { duration: StageTime, target: testStages[5] },
        { duration: StageTime, target: testStages[6] },

        { duration: StageTime, target: testStages[7] },
        { duration: StageTime, target: testStages[8] },
        { duration: StageTime, target: testStages[9] },
        { duration: StageTime, target: testStages[10] },
        { duration: StageTime, target: testStages[11] },
        { duration: StageTime, target: testStages[12] },
        { duration: StageTime, target: testStages[13] },
        { duration: StageTime, target: testStages[14] },
        { duration: StageTime, target: testStages[15] },
        { duration: StageTime, target: testStages[16] },

        { duration: StageTime, target: testStages[17] },
        { duration: StageTime, target: testStages[18] },
        { duration: StageTime, target: testStages[19] },
        { duration: StageTime, target: testStages[20] },
        { duration: StageTime, target: testStages[21] },
        { duration: StageTime, target: testStages[22] },
        { duration: StageTime, target: testStages[23] },
        { duration: StageTime, target: testStages[24] },
        { duration: StageTime, target: testStages[25] },
        { duration: StageTime, target: testStages[26] },

        { duration: StageTime, target: testStages[27] },
        { duration: StageTime, target: testStages[28] },
        { duration: StageTime, target: testStages[29] },
        { duration: StageTime, target: testStages[30] },
        { duration: StageTime, target: testStages[31] },
        { duration: StageTime, target: testStages[32] },
        { duration: StageTime, target: testStages[33] },
        { duration: StageTime, target: testStages[34] },
        { duration: StageTime, target: testStages[35] },
        { duration: StageTime, target: testStages[36] },

        { duration: StageTime, target: testStages[37] },
        { duration: StageTime, target: testStages[38] },
        { duration: StageTime, target: testStages[39] },
        { duration: StageTime, target: testStages[40] },
        { duration: StageTime, target: testStages[41] },
        { duration: StageTime, target: testStages[42] },
        { duration: StageTime, target: testStages[43] },
        { duration: StageTime, target: testStages[44] },
        { duration: StageTime, target: testStages[45] },
        { duration: StageTime, target: testStages[46] },

        { duration: StageTime, target: testStages[47] },
        { duration: StageTime, target: testStages[48] },
        { duration: StageTime, target: testStages[49] },
        { duration: StageTime, target: testStages[50] },
        { duration: StageTime, target: testStages[51] },
        { duration: StageTime, target: testStages[52] },
        { duration: StageTime, target: testStages[53] },
        { duration: StageTime, target: testStages[54] },
        { duration: StageTime, target: testStages[55] },
      ],
    },
  },
}

export function setup() {
  const testStartTime = new Date();
  console.warn("Test Start Time:" + testStartTime.toLocaleTimeString('en-US'));
  console.warn("Test Start Time(UTC):" + testStartTime.toUTCString());
  let delayMap = logParser.debugDelay(loadProfile);
  return delayMap;
}

export function teardown() {
  const testEndTime = new Date();
  console.warn("Test End Time:" + testEndTime.toLocaleTimeString('en-US'));
  console.warn("Test End Time(UTC):" + testEndTime.toUTCString());
}

export default function main(delayMap) {
  logIterator(delayMap)
}

export function logIterator(delayMap) {
  let selectLoadProfile = logParser.getRequestDetailsFromLog(loadProfile, scenario.iterationInTest)
  let reqData = logParser.filterRequestType(selectLoadProfile)
  let delay = delayMap[scenario.iterationInTest].toFixed(2);
  sleep(delay)
  essAPI.ReplayRequest(reqData);
  sleep(StageTime)
}

export function handleSummary(data) {
  return {
    ["ExecutionSummary/ESSPT-Baseline-LiveDataTest-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["ExecutionSummary/ESSPT-Baseline-LiveDataTest-" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}