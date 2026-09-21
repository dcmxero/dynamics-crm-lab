/*
 * ATTENTION: The "eval" devtool has been used (maybe by default in mode: "development").
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
var pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad;
/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./WorkOrderStatusTrack/StatusTrack.tsx"
/*!**********************************************!*\
  !*** ./WorkOrderStatusTrack/StatusTrack.tsx ***!
  \**********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

eval("{__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   StatusTrack: () => (/* binding */ StatusTrack)\n/* harmony export */ });\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! react */ \"react\");\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(react__WEBPACK_IMPORTED_MODULE_0__);\n\nvar DOT_COLORS = {\n  done: \"#107c10\",\n  current: \"#0f6cbd\",\n  upcoming: \"#c8c6c4\"\n};\n/**\n * Draws the lifecycle as a row of stages with the current one picked out.\n *\n * Presentation only: everything arrives through props, so there is no state to\n * get out of step with the record.\n */\nvar StatusTrack = _ref => {\n  var stages = _ref.stages,\n    label = _ref.label;\n  var currentIndex = stages.findIndex(stage => stage.state === \"current\");\n  return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n    role: \"group\",\n    \"aria-label\": label,\n    style: {\n      display: \"flex\",\n      alignItems: \"flex-start\",\n      fontFamily: \"Segoe UI, sans-serif\",\n      fontSize: 12\n    }\n  }, stages.map((stage, index) => (/*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n    key: stage.value,\n    style: {\n      flex: 1,\n      minWidth: 0\n    }\n  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n    style: {\n      display: \"flex\",\n      alignItems: \"center\"\n    }\n  }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"span\", {\n    \"aria-hidden\": \"true\",\n    style: {\n      width: 10,\n      height: 10,\n      borderRadius: \"50%\",\n      background: DOT_COLORS[stage.state],\n      flex: \"0 0 auto\"\n    }\n  }), index < stages.length - 1 && (/*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"span\", {\n    \"aria-hidden\": \"true\",\n    style: {\n      height: 2,\n      flex: 1,\n      background: index < currentIndex ? DOT_COLORS.done : DOT_COLORS.upcoming\n    }\n  }))), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n    style: {\n      marginTop: 4,\n      paddingRight: 8,\n      color: stage.state === \"upcoming\" ? \"#605e5c\" : \"#201f1e\",\n      fontWeight: stage.state === \"current\" ? 600 : 400,\n      overflow: \"hidden\",\n      textOverflow: \"ellipsis\",\n      whiteSpace: \"nowrap\"\n    },\n    \"aria-current\": stage.state === \"current\" ? \"step\" : undefined\n  }, stage.label)))));\n};\n\n//# sourceURL=webpack://pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad/./WorkOrderStatusTrack/StatusTrack.tsx?\n}");

/***/ },

/***/ "./WorkOrderStatusTrack/index.ts"
/*!***************************************!*\
  !*** ./WorkOrderStatusTrack/index.ts ***!
  \***************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

eval("{__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   WorkOrderStatusTrack: () => (/* binding */ WorkOrderStatusTrack)\n/* harmony export */ });\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! react */ \"react\");\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(react__WEBPACK_IMPORTED_MODULE_0__);\n/* harmony import */ var _StatusTrack__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./StatusTrack */ \"./WorkOrderStatusTrack/StatusTrack.tsx\");\n/* harmony import */ var _workOrderStatus__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./workOrderStatus */ \"./WorkOrderStatusTrack/workOrderStatus.ts\");\n\n\n\n/**\n * Binds the control to the Power Apps component framework.\n *\n * The class does nothing but talk to the host: read the bound value, hand back\n * the outputs. Working out the stages lives in workOrderStatus.ts and drawing\n * them in StatusTrack.tsx.\n */\nclass WorkOrderStatusTrack {\n  constructor() {\n    this.status = null;\n  }\n  init(context, notifyOutputChanged, state) {\n    // A virtual control owns no DOM; the host renders whatever updateView returns.\n  }\n  updateView(context) {\n    var _a, _b, _c;\n    this.status = (_a = context.parameters.status.raw) !== null && _a !== void 0 ? _a : null;\n    return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(_StatusTrack__WEBPACK_IMPORTED_MODULE_1__.StatusTrack, {\n      stages: (0,_workOrderStatus__WEBPACK_IMPORTED_MODULE_2__.describeStages)(this.status),\n      label: (_c = (_b = context.parameters.status.attributes) === null || _b === void 0 ? void 0 : _b.DisplayName) !== null && _c !== void 0 ? _c : \"Work order status\"\n    });\n  }\n  getOutputs() {\n    var _a;\n    // The control only shows the value, so it hands it back unchanged.\n    return {\n      status: (_a = this.status) !== null && _a !== void 0 ? _a : undefined\n    };\n  }\n  destroy() {\n    // No listeners or timers were attached, so there is nothing to tidy up.\n  }\n}\n\n//# sourceURL=webpack://pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad/./WorkOrderStatusTrack/index.ts?\n}");

/***/ },

/***/ "./WorkOrderStatusTrack/workOrderStatus.ts"
/*!*************************************************!*\
  !*** ./WorkOrderStatusTrack/workOrderStatus.ts ***!
  \*************************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

eval("{__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   WORK_ORDER_STAGES: () => (/* binding */ WORK_ORDER_STAGES),\n/* harmony export */   describeStages: () => (/* binding */ describeStages),\n/* harmony export */   progressOf: () => (/* binding */ progressOf)\n/* harmony export */ });\n/**\n * The lifecycle of a work order, mirrored from the domain enum so the control\n * shows the same stages the server enforces.\n *\n * Deliberately free of React and of the Power Apps framework: it is plain data\n * and plain functions, which makes it the part that can be reasoned about and\n * tested on its own.\n */\nvar WORK_ORDER_STAGES = [{\n  value: 1,\n  label: \"New\"\n}, {\n  value: 2,\n  label: \"Assigned\"\n}, {\n  value: 3,\n  label: \"In progress\"\n}, {\n  value: 4,\n  label: \"Closed\"\n}];\n/**\n * Describes each stage relative to the one the job has reached.\n *\n * An unknown or missing value leaves every stage upcoming rather than guessing,\n * so a choice column that gained a new option does not silently mislead.\n */\nfunction describeStages(current) {\n  var currentIndex = WORK_ORDER_STAGES.findIndex(stage => stage.value === current);\n  return WORK_ORDER_STAGES.map((stage, index) => ({\n    value: stage.value,\n    label: stage.label,\n    state: stateOf(index, currentIndex)\n  }));\n}\n/**\n * Reports how far along the lifecycle the job is, as a fraction between 0 and 1.\n */\nfunction progressOf(current) {\n  var currentIndex = WORK_ORDER_STAGES.findIndex(stage => stage.value === current);\n  if (currentIndex < 0) {\n    return 0;\n  }\n  return currentIndex / (WORK_ORDER_STAGES.length - 1);\n}\nfunction stateOf(index, currentIndex) {\n  if (currentIndex < 0 || index > currentIndex) {\n    return \"upcoming\";\n  }\n  return index === currentIndex ? \"current\" : \"done\";\n}\n\n//# sourceURL=webpack://pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad/./WorkOrderStatusTrack/workOrderStatus.ts?\n}");

/***/ },

/***/ "react"
/*!***************************!*\
  !*** external "Reactv16" ***!
  \***************************/
(module) {

module.exports = Reactv16;

/***/ }

/******/ 	});
/************************************************************************/
/******/ 	// The module cache
/******/ 	const __webpack_module_cache__ = {};
/******/ 	
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/ 		// Check if module is in cache
/******/ 		const cachedModule = __webpack_module_cache__[moduleId];
/******/ 		if (cachedModule !== undefined) {
/******/ 			return cachedModule.exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		const module = __webpack_module_cache__[moduleId] = {
/******/ 			// no module.id needed
/******/ 			// no module.loaded needed
/******/ 			exports: {}
/******/ 		};
/******/ 	
/******/ 		// Execute the module function
/******/ 		if (!(moduleId in __webpack_modules__)) {
/******/ 			delete __webpack_module_cache__[moduleId];
/******/ 			const e = new Error("Cannot find module '" + moduleId + "'");
/******/ 			e.code = 'MODULE_NOT_FOUND';
/******/ 			throw e;
/******/ 		}
/******/ 		__webpack_modules__[moduleId](module, module.exports, __webpack_require__);
/******/ 	
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/ 	
/************************************************************************/
/******/ 	/* webpack/runtime/compat get default export */
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = (module) => {
/******/ 		const getter = module && module.__esModule ?
/******/ 			() => (module['default']) :
/******/ 			() => (module);
/******/ 		__webpack_require__.d(getter, { a: getter });
/******/ 		return getter;
/******/ 	};
/******/ 	
/******/ 	/* webpack/runtime/define property getters */
/******/ 	// define getter/value functions for harmony exports
/******/ 	__webpack_require__.d = (exports, definition) => {
/******/ 		for(var key in definition) {
/******/ 			if(__webpack_require__.o(definition, key) && !__webpack_require__.o(exports, key)) {
/******/ 				Object.defineProperty(exports, key, { enumerable: true, get: definition[key] });
/******/ 			}
/******/ 		}
/******/ 	};
/******/ 	
/******/ 	/* webpack/runtime/hasOwnProperty shorthand */
/******/ 	__webpack_require__.o = (obj, prop) => (Object.prototype.hasOwnProperty.call(obj, prop));
/******/ 	
/******/ 	/* webpack/runtime/make namespace object */
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = (exports) => {
/******/ 		Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
/******/ 	};
/******/ 	
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module can't be inlined because the eval devtool is used.
/******/ 	let __webpack_exports__ = __webpack_require__("./WorkOrderStatusTrack/index.ts");
/******/ 	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = __webpack_exports__;
/******/ 	
/******/ })()
;
if (window.ComponentFramework && window.ComponentFramework.registerControl) {
	ComponentFramework.registerControl('DynamicsCrmLab.WorkOrderStatusTrack', pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.WorkOrderStatusTrack);
} else {
	var DynamicsCrmLab = DynamicsCrmLab || {};
	DynamicsCrmLab.WorkOrderStatusTrack = pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.WorkOrderStatusTrack;
	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = undefined;
}