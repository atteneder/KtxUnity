cmake_minimum_required(VERSION 3.0)

project(basisu_transcoder)

if(APPLE)
    # This little macro lets you set any XCode specific property
    macro (set_xcode_property TARGET XCODE_PROPERTY XCODE_VALUE)
        set_property (TARGET ${TARGET} PROPERTY XCODE_ATTRIBUTE_${XCODE_PROPERTY} ${XCODE_VALUE})
    endmacro (set_xcode_property)
endif(APPLE)

set(BASISU_SRC_LIST
	basis_universal/transcoder/basisu_transcoder.cpp
	basisu_wrapper/basisu_wrapper.cpp
	)

if( APPLE_IOS )
	set( LibType STATIC )
else( APPLE_IOS )
	# Unity Editor (on macOS) requires a dynamic library.
	set( LibType SHARED )
endif()

add_library(basisu ${LibType} ${BASISU_SRC_LIST})

target_include_directories(
	basisu
	PUBLIC
	basis_universal/transcoder
)

if(APPLE AND NOT APPLE_IOS)
	# Unity Editor need the library in .bundle format:
	set (CMAKE_SHARED_LIBRARY_PREFIX "")
    set (CMAKE_SHARED_MODULE_PREFIX "")

    set (CMAKE_STATIC_LIBRARY_SUFFIX ".bundle")
    set (CMAKE_SHARED_LIBRARY_SUFFIX ".bundle")
    
	set_xcode_property (basisu EXECUTABLE_SUFFIX ".bundle")
endif()

set(UNITY_PLUGIN_DIR ${CMAKE_SOURCE_DIR}/BasisUniversalUnity/Assets/Plugins/x86_64)
set(UNITY_PLUGIN_DIR_WEBGL ${CMAKE_SOURCE_DIR}/BasisUniversalUnity/Assets/Plugins/WebGL)
set(UNITY_STREAMING_ASSETS_DIR ${CMAKE_SOURCE_DIR}/BasisUniversalUnity/Assets/StreamingAssets)

install(TARGETS basisu DESTINATION ${UNITY_PLUGIN_DIR})

install(
FILES
	basis_universal/transcoder/basisu_transcoder.h
	basis_universal/transcoder/basisu_transcoder_internal.h
	basis_universal/transcoder/basisu_global_selector_cb.h
	basis_universal/transcoder/basisu_transcoder.cpp
	basis_universal/transcoder/basisu_transcoder_tables_bc7_m6.inc
	basis_universal/transcoder/basisu_global_selector_palette.h
	basis_universal/transcoder/basisu.h
	basis_universal/transcoder/basisu_transcoder_tables_dxt1_6.inc
	basis_universal/transcoder/basisu_file_headers.h
	basis_universal/transcoder/basisu_transcoder_tables_dxt1_5.inc
	basisu_wrapper/basisu_wrapper.cpp
DESTINATION
	${UNITY_PLUGIN_DIR_WEBGL}
)

install(
FILES
	basis_universal/webgl/texture/assets/kodim20.basis
	basis_universal/webgl/texture/assets/alpha3.basis
DESTINATION
	${UNITY_STREAMING_ASSETS_DIR}
)

