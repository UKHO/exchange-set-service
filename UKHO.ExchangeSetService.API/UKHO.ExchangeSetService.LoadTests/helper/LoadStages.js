const logParser = require('./LogParser.js');
/**
 * @param {Object} IterationCount The number of iteration to be distributed 
 * @returns {Array} Request rate per stage required to complete the iterations  
*/
export function getLoadStages(logFile) {
    let reqRate = logParser.requestRatePerMinute(logFile);
    const testStages = Object.values(reqRate);
    return testStages;
}
