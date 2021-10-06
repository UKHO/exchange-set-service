import http from "k6/http";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';
import { sleep } from 'k6';
import { Trend } from 'k6/metrics';

const runTestProductIdentifier = require('./scripts/LoadTestForProductIdentifier.js');
const config = JSON.parse(open('./config.json'));
const dataHelper = require('./helper/dataHelper.js');
const apiClient = require('./helper/clientHelper.js');

const productIdentifierData_Small_25MB = dataHelper.GetProductIdentifierDataforSmallExchangeSet_25MB();
const productIdentifierData_Small_50MB = dataHelper.GetProductIdentifierDataforSmallExchangeSet_50MB();
const productIdentifierData_Medium_150MB = dataHelper.GetProductIdentifierDataforMediumExchangeSet_150MB();
const productIdentifierData_Medium_300MB = dataHelper.GetProductIdentifierDataforMediumExchangeSet_300MB();
const productIdentifierData_Large = dataHelper.GetProductIdentifierDataforLargeExchangeSet();

let SmallExchangeSetCreationTrendfor25MB = new Trend('SmallEssCreationtimefor25MB');
let SmallExchangeSetCreationTrendfor50MB = new Trend('SmallEssCreationtimefor50MB');
let MediumExchangeSetCreationTrendfor150MB = new Trend('MediumEssCreationtimefor150MB');
let MediumExchangeSetCreationTrendfor300MB = new Trend('MediumEssCreationtimefor300MB');
let LargeExchangeSetCreationTrend = new Trend('LargeEssCreationtime');
let clientAuthResp = {};

export let options = {
    scenarios: {
      ESSCreationSmallExchangeSet_25MB: {
        exec: 'ESSCreationSmallExchangeSet_25MB',
        executor: 'ramping-vus',
        stages:  [
          { duration: '5m', target: 10 },
          { duration: '5m', target: 10 },
          { duration: '5m', target: 30 },
          { duration: '35m', target: 30 },
          { duration: '5m', target: 20 },
          { duration: '5m', target: 0 }
        ]
      },
      ESSCreationSmallExchangeSet_50MB: {
        exec: 'ESSCreationSmallExchangeSet_50MB',
        executor: 'ramping-vus',
        stages: [
          { duration: '5m', target: 5 },
          { duration: '5m', target: 5 },
          { duration: '5m', target: 10 },
          { duration: '35m', target: 10 },
          { duration: '5m', target: 5 },
          { duration: '5m', target: 0 }
        ]
      },
      ESSCreationMediumExchangeSet_150MB: {
        exec: 'ESSCreationMediumExchangeSet_150MB',
        executor: 'ramping-vus',
        stages: [
          { duration: '5m', target: 2 },
          { duration: '5m', target: 2 },
          { duration: '5m', target: 5 },
          { duration: '35m', target: 5 },
          { duration: '5m', target: 2 },
          { duration: '5m', target: 0 }
        ]
      },
      ESSCreationMediumExchangeSet_300MB: {
        exec: 'ESSCreationMediumExchangeSet_300MB',
        executor: 'ramping-vus',
        stages: [
          { duration: '5m', target: 1 },
          { duration: '5m', target: 1 },
          { duration: '5m', target: 2 },
          { duration: '35m', target: 2 },
          { duration: '5m', target: 1 },
          { duration: '5m', target: 0 }
        ]
      },
      ESSCreationLargeExchangeSet: {
        exec: 'ESSCreationLargeExchangeSet',
        executor: 'ramping-vus',
        stages: [
          { duration: '10m', target: 1 },
          { duration: '10m', target: 1 },
          { duration: '10m', target: 2 },
          { duration: '20m', target: 2 },
          { duration: '5m', target: 1 },
          { duration: '5m', target: 0 }
          ]
    },
  },
  
  };

export function setup() {
     // client credentials authentication flow
     let essAuthResp = authenticateUsingAzure(
         `${config.ESS_TENANT_ID}`, `${config.ESS_CLIENT_ID}`, `${config.ESS_CLIENT_SECRET}`, `${config.ESS_SCOPES}`, `${config.ESS_RESOURCE}`
     );
    clientAuthResp["essToken"] = essAuthResp.access_token;

     let fssAuthResp = authenticateUsingAzure(
         `${config.FSS_TENANT_ID}`, `${config.FSS_CLIENT_ID}`, `${config.FSS_CLIENT_SECRET}`, `${config.FSS_SCOPES}`, `${config.FSS_RESOURCE}`
     );
    clientAuthResp["fssToken"] = fssAuthResp.access_token;

    return clientAuthResp;
}

export function ESSCreationSmallExchangeSet_25MB(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('Small_25MB_EssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Small_25MB, "Small_25MB");
    });
    SmallExchangeSetCreationTrendfor25MB.add(group_duration);
    sleep(1);
}

export function ESSCreationSmallExchangeSet_50MB(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('Small_50MB_EssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Small_50MB, "Small_50MB");
    });
    SmallExchangeSetCreationTrendfor50MB.add(group_duration);
    sleep(1);
}

export function ESSCreationMediumExchangeSet_150MB(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('Medium_150MB_EssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Medium_150MB, "Medium_150MB");
    });
    MediumExchangeSetCreationTrendfor150MB.add(group_duration);
    sleep(1);
}

export function ESSCreationMediumExchangeSet_300MB(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('Medium_300MB_EssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Medium_300MB, "Medium_300MB");
    });
    MediumExchangeSetCreationTrendfor300MB.add(group_duration);
    sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('LargeEssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Large, "Large");
    });
    LargeExchangeSetCreationTrend.add(group_duration);
    sleep(1);
}


export function handleSummary(data) {
    return {
        ["summary/ProductIdentifierResult_"+new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]:htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
        ["summary/ProductIdentifierResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]:JSON.stringify(data),
    }
}