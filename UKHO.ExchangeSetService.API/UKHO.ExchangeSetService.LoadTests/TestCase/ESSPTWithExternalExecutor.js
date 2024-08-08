import http from 'k6/http';
import { sleep, metrics, output } from 'k6';
import { vu, scenario, exec, instance } from 'k6/execution';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { Counter } from 'k6/metrics';

const logParser = require('../helper/LogParser.js');
const loadProfile = JSON.parse(open('./LiveLogs/filtered.json'));
const essAPI = require('../scripts/ReplayRequest.js');
let logsCount = new Counter("http_reqs")

export let options = {

  scenarios: {
    ReplayLiveTraffic_ThreadOne: {
      exec: 'simulateProdTrafficOnGivenDate',
      executor: 'shared-iterations',
      vus: 10,
      iterations: loadProfile.length,
      maxDuration: '1h',
    },
  },
};

export function setup() {
  const testStartTime = new Date();
  console.warn("Test Start Time:" + testStartTime.toLocaleTimeString('en-US'));
  console.warn("Test Start Time(UTC):" + testStartTime.toUTCString());

  const RPM = logParser.requestRatePerMinute(loadProfile)

  console.log(Object.keys(RPM).length)

  let delayMap = logParser.debugDelay(loadProfile);
  return delayMap;
}

export function teardown() {
  const testEndTime = new Date();
  console.warn("Test End Time:" + testEndTime.toLocaleTimeString('en-US'));
  console.warn("Test End Time(UTC):" + testEndTime.toUTCString());
}

export function simulateProdTrafficOnGivenDate(delayMap) {
  let selectLoadProfile = logParser.getRequestDetailsFromLog(loadProfile, scenario.iterationInTest)
  let reqData = logParser.filterRequestType(selectLoadProfile)
  let LogsDelayProfile = delayMap[logsCount].toFixed(2);
  console.log("Iteration:" + scenario.iterationInTest, "VU:" + LogsDelayProfile)
  sleep(LogsDelayProfile)
  essAPI.ReplayRequest(reqData);
}

export function handleSummary(data) {
  return {
    ["ExecutionSummary/PT03June_Baseline-Live-R1" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["ExecutionSummary/PT03June_Baseline-Live-R1" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}

