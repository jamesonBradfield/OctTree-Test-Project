#!/bin/zsh

# Check if required parameters are provided
if [ $# -lt 2 ]; then
    echo "Usage: $0 <scene-path> <duration-in-seconds> [output-file]"
    echo "Example: $0 res://my_scene.tscn 30 my_trace"
    exit 1
fi

SCENE_PATH="$1"
DURATION="$2"
OUTPUT_NAME="${3:-trace}"

echo "Starting Godot with scene: $SCENE_PATH"
echo "Will trace for $DURATION seconds"

# Start Godot with nohup to ensure it doesn't block our script
nohup godot-mono "$SCENE_PATH" > godot_trace_run.log 2>&1 &
GODOT_PID=$!

echo "Godot started with PID: $GODOT_PID"

# Wait a moment and check if Godot is still running
sleep 3
if ! ps -p $GODOT_PID > /dev/null; then
    echo "Error: Godot process exited prematurely. Check godot_trace_run.log for details."
    cat godot_trace_run.log
    exit 1
fi

# Use the cpu-sampling profile which is more complete
echo "Starting dotnet-trace with CPU sampling profile..."
dotnet-trace collect --process-id $GODOT_PID --profile cpu-sampling --format speedscope --output "$OUTPUT_NAME.speedscope" &
TRACE_PID=$!

# Wait for the specified duration
echo "Tracing for $DURATION seconds..."
sleep $DURATION

# Check if Godot is still running
if ps -p $GODOT_PID > /dev/null; then
    # Send SIGINT to the dotnet-trace process to stop collection
    echo "Stopping trace collection..."
    kill -SIGINT $TRACE_PID

    # Wait a moment for dotnet-trace to finish
    sleep 2
    
    # Kill the Godot process
    echo "Stopping Godot..."
    kill $GODOT_PID 2>/dev/null
    
    # Check if the trace files were created
    if [ -f "$OUTPUT_NAME.nettrace" ] || [ -f "$OUTPUT_NAME.speedscope.json" ]; then
        echo "Trace collected successfully."
        echo "Files saved to:"
        [ -f "$OUTPUT_NAME.nettrace" ] && echo "  - $OUTPUT_NAME.nettrace (Original format)"
        [ -f "$OUTPUT_NAME.speedscope.json" ] && echo "  - $OUTPUT_NAME.speedscope.json (Speedscope format)"
        [ -f "$OUTPUT_NAME.speedscope" ] && echo "  - $OUTPUT_NAME.speedscope (Intermediate format)"
        echo ""
        echo "You can view the speedscope file at https://www.speedscope.app"
        echo "If the file appears empty in speedscope.app, try the following alternative command:"
        echo "dotnet-trace collect --process-id \$PID --providers Microsoft-Windows-DotNETRuntime:4:4,Microsoft-DotNETCore-SampleProfiler:4:4 --format speedscope"
    else
        echo "Warning: Trace files not found. Check for errors in the output above."
    fi
else
    echo "Warning: Godot process exited during tracing."
    # Try to stop dotnet-trace
    kill -SIGINT $TRACE_PID 2>/dev/null
fi

# Clean up any remaining processes
wait $TRACE_PID 2>/dev/null
wait $GODOT_PID 2>/dev/null

echo "Done."
