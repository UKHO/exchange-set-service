import http from "k6/http";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';
import { sleep, group } from 'k6';
import { Trend } from 'k6/metrics';

const config = JSON.parse(open('./config.json'));
const dataHelper = require('./Helper/DataHelper.js');
const apiClient = require('./Helper/ClientHelper.js');

const productIdentifierData_Small_25MB = dataHelper.GetProductIdentifierDataforSmallExchangeSet_25MB();
const productIdentifierData_Small_50MB = dataHelper.GetProductIdentifierDataforSmallExchangeSet_50MB();
const productIdentifierData_Medium_150MB = dataHelper.GetProductIdentifierDataforMediumExchangeSet_150MB();
const productIdentifierData_Medium_300MB = dataHelper.GetProductIdentifierDataforMediumExchangeSet_300MB();
const productIdentifierData_Large = dataHelper.GetProductIdentifierDataforLargeExchangeSet();

let clientAuthResp = {};
let essResponse;

export let options = {
    scenarios: {
        ESSCreationSmallExchangeSet_25MB: {
            exec: 'ESSCreationSmallExchangeSet_25MB',
            executor: 'per-vu-iterations',
            startTime: '5s',
            gracefulStop: '5s',
            vus: 5,
            iterations: 25,
            maxDuration: '1h'
        },

        ESSCreationSmallExchangeSet_50MB: {
            exec: 'ESSCreationSmallExchangeSet_50MB',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 5,
            iterations: 25,
            maxDuration: '1h'
        },

        ESSCreationMediumExchangeSet_150MB: {
            exec: 'ESSCreationMediumExchangeSet_150MB',
            executor: 'per-vu-iterations',
            startTime: '15s',
            gracefulStop: '5s',
            vus: 5,
            iterations: 10,
            maxDuration: '1h'
        },

        ESSCreationMediumExchangeSet_300MB: {
            exec: 'ESSCreationMediumExchangeSet_300MB',
            executor: 'per-vu-iterations',
            startTime: '20s',
            gracefulStop: '5s',
            vus: 5,
            iterations: 5,
            maxDuration: '1h'
        },

        ESSCreationLargeExchangeSet: {
            exec: 'ESSCreationLargeExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '25',
            gracefulStop: '5s',
            vus: 5,
            iterations: 3,
            maxDuration: '1h'
        },
    },
};

export function setup() {
     //client credentials authentication flow
     let essAuthResp = authenticateUsingAzure(
         `${config.ESS_TENANT_ID}`, `${config.ESS_CLIENT_ID}`, `${config.ESS_CLIENT_SECRET}`, `${config.ESS_SCOPES}`, `${config.ESS_RESOURCE}`
     );
    clientAuthResp["essToken"] = essAuthResp.access_token;

    return clientAuthResp;
}

export function ESSCreationSmallExchangeSet_25MB(clientAuthResp) {
    group('Small_25MB_EssCreation', () => {
        essResponse = apiClient.GetESSApiResponse("productInformation/productIdentifiers", productIdentifierData_Small_25MB, `${clientAuthResp.essToken}`, "Small_25MB");
  });
  
  sleep(1);
}

export function ESSCreationSmallExchangeSet_50MB(clientAuthResp) {
  
    group('Small_50MB_EssCreation', () => {
        essResponse = apiClient.GetESSApiResponse("productInformation/productIdentifiers", productIdentifierData_Small_50MB, `${clientAuthResp.essToken}`, "Small_50MB");
  });

  sleep(1);
}

export function ESSCreationMediumExchangeSet_150MB(clientAuthResp) {
    group('Medium_150MB_EssCreation', () => {
        essResponse = apiClient.GetESSApiResponse("productInformation/productIdentifiers", productIdentifierData_Medium_150MB, `${clientAuthResp.essToken}`, "Medium_150MB");
  });

  sleep(1);
}

export function ESSCreationMediumExchangeSet_300MB(clientAuthResp) {
    group('Medium_300MB_EssCreation', () => {
        essResponse = apiClient.GetESSApiResponse("productInformation/productIdentifiers", productIdentifierData_Medium_300MB, `${clientAuthResp.essToken}`, "Medium_300MB");
  });

  sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
    group('LargeEssCreation', () => {
        essResponse = apiClient.GetESSApiResponse("productInformation/productIdentifiers", productIdentifierData_Large, `${clientAuthResp.essToken}`, "Large");
  });

  sleep(1);
}

export function handleSummary(data) {
  return {
    ["summary/ProductInformation/ProductIdentifierResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
    ["summary/ProductInformation/ProductIdentifierResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
  }
}