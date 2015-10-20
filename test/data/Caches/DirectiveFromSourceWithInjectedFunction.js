angular.module('foo').directive('sample', ['$foo', function factory($foo) {

    return {
        restrict: 'A'
    };
}]);
